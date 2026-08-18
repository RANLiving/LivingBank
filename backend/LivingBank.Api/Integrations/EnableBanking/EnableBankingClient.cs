using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LivingBank.Api.Configuration;
using Microsoft.Extensions.Options;

namespace LivingBank.Api.Integrations.EnableBanking;

public interface IEnableBankingClient
{
    Task<EbAccountsResponse> GetAccountsAsync(string sessionId, CancellationToken ct = default);
    Task<EbBalancesResponse> GetBalancesAsync(string accountUid, CancellationToken ct = default);
    Task<EbTransactionsResponse> GetTransactionsAsync(string accountUid, DateOnly? dateFrom, string? continuationKey, CancellationToken ct = default);
    Task<List<EbAspspDto>> GetAspspsAsync(string country, CancellationToken ct = default);
    Task<string> StartAuthorizationAsync(string aspspName, string aspspCountry, string redirectUrl, string state, int validDays, CancellationToken ct = default);
    Task<EbSessionResponse> CreateSessionAsync(string authorizationCode, CancellationToken ct = default);
}

/// <summary>
/// Cliente HTTP para a API Enable Banking. Autentica cada pedido com um JWT
/// assinado (RS256) pela chave privada da aplicação, conforme o fluxo documentado em
/// https://enablebanking.com/docs/api/reference/ — validar campos exatos antes de produção.
/// </summary>
public class EnableBankingClient : IEnableBankingClient
{
    private readonly HttpClient _http;
    private readonly EnableBankingOptions _options;

    public EnableBankingClient(HttpClient http, IOptions<EnableBankingOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.BaseAddress = new Uri(_options.BaseUrl);
    }

    private void ApplyAuthHeader()
    {
        var jwt = BuildApplicationJwt();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
    }

    /// <summary>
    /// Assina o JWT de autenticação da aplicação manualmente (RS256), em vez de usar
    /// JwtSecurityTokenHandler/SigningCredentials: o pipeline de assinatura do
    /// Microsoft.IdentityModel.Tokens toma posse da chave RSA fornecida e dispõe-a após o
    /// primeiro uso, o que rebenta em pedidos seguintes ("Cannot access a disposed object
    /// RSAOpenSsl"). Assinar à mão evita essa gestão de ciclo de vida por completo.
    /// </summary>
    private string BuildApplicationJwt()
    {
        if (string.IsNullOrWhiteSpace(_options.PrivateKeyPem))
            throw new InvalidOperationException("EnableBanking:PrivateKeyPem não está configurado.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.PrivateKeyPem);

        var now = DateTimeOffset.UtcNow;
        var header = JsonSerializer.Serialize(new { alg = "RS256", typ = "JWT", kid = _options.ApplicationId });
        var payload = JsonSerializer.Serialize(new
        {
            iss = "enablebanking.com",
            aud = "api.enablebanking.com",
            iat = now.ToUnixTimeSeconds(),
            nbf = now.ToUnixTimeSeconds(),
            exp = now.AddMinutes(5).ToUnixTimeSeconds()
        });

        var signingInput = $"{Base64UrlEncode(Encoding.UTF8.GetBytes(header))}.{Base64UrlEncode(Encoding.UTF8.GetBytes(payload))}";
        var signature = rsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return $"{signingInput}.{Base64UrlEncode(signature)}";
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct);
        throw new HttpRequestException($"Enable Banking devolveu {(int)response.StatusCode} {response.StatusCode}: {body}");
    }

    public async Task<EbAccountsResponse> GetAccountsAsync(string sessionId, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var sessionResponse = await _http.GetAsync($"/sessions/{sessionId}", ct);
        await EnsureSuccessAsync(sessionResponse, ct);
        var session = await sessionResponse.Content.ReadFromJsonAsync<EbSessionResponse>(cancellationToken: ct) ?? new EbSessionResponse();

        if (session.AccountsData is { Count: > 0 })
            return new EbAccountsResponse { Accounts = session.AccountsData };

        var accounts = new List<EbAccountDto>();
        foreach (var uid in session.AccountUids)
        {
            ApplyAuthHeader();
            var accResponse = await _http.GetAsync($"/accounts/{uid}", ct);
            if (!accResponse.IsSuccessStatusCode) continue;
            var acc = await accResponse.Content.ReadFromJsonAsync<EbAccountDto>(cancellationToken: ct);
            if (acc is not null) accounts.Add(acc);
        }
        return new EbAccountsResponse { Accounts = accounts };
    }

    public async Task<EbBalancesResponse> GetBalancesAsync(string accountUid, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _http.GetAsync($"/accounts/{accountUid}/balances", ct);
        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<EbBalancesResponse>(cancellationToken: ct);
        return result ?? new EbBalancesResponse();
    }

    public async Task<EbTransactionsResponse> GetTransactionsAsync(string accountUid, DateOnly? dateFrom, string? continuationKey, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var query = new StringBuilder($"/accounts/{accountUid}/transactions?");
        if (dateFrom.HasValue) query.Append($"date_from={dateFrom.Value:yyyy-MM-dd}&");
        if (!string.IsNullOrEmpty(continuationKey)) query.Append($"continuation_key={Uri.EscapeDataString(continuationKey)}&");

        var response = await _http.GetAsync(query.ToString(), ct);
        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<EbTransactionsResponse>(cancellationToken: ct);
        return result ?? new EbTransactionsResponse();
    }

    public async Task<List<EbAspspDto>> GetAspspsAsync(string country, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _http.GetAsync($"/aspsps?country={Uri.EscapeDataString(country)}", ct);
        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<EbAspspsResponse>(cancellationToken: ct);
        return result?.Aspsps ?? [];
    }

    /// <summary>
    /// Inicia o fluxo de consentimento PSD2: devolve o URL para onde o utilizador deve ser
    /// redirecionado para autenticar e autorizar no banco (ASPSP). Depois de autorizar, o
    /// ASPSP redireciona de volta para <paramref name="redirectUrl"/> com "?code=...&amp;state=...".
    /// </summary>
    public async Task<string> StartAuthorizationAsync(string aspspName, string aspspCountry, string redirectUrl, string state, int validDays, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var payload = new
        {
            access = new { valid_until = DateTime.UtcNow.AddDays(validDays).ToString("o") },
            aspsp = new { name = aspspName, country = aspspCountry },
            state,
            redirect_url = redirectUrl,
            psu_type = "personal"
        };

        var response = await _http.PostAsJsonAsync("/auth", payload, ct);
        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<EbAuthorizeResponse>(cancellationToken: ct);
        return result?.Url ?? throw new InvalidOperationException("Enable Banking não devolveu um URL de autorização.");
    }

    /// <summary>Troca o "code" recebido no callback por uma sessão válida com a lista de contas autorizadas.</summary>
    public async Task<EbSessionResponse> CreateSessionAsync(string authorizationCode, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _http.PostAsJsonAsync("/sessions", new { code = authorizationCode }, ct);
        await EnsureSuccessAsync(response, ct);
        var result = await response.Content.ReadFromJsonAsync<EbSessionResponse>(cancellationToken: ct);
        return result ?? new EbSessionResponse();
    }
}
