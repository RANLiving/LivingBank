using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using LivingBank.Api.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

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

    private string BuildApplicationJwt()
    {
        if (string.IsNullOrWhiteSpace(_options.PrivateKeyPem))
            throw new InvalidOperationException("EnableBanking:PrivateKeyPem não está configurado.");

        using var rsa = RSA.Create();
        rsa.ImportFromPem(_options.PrivateKeyPem);
        var signingKey = new RsaSecurityKey(rsa) { KeyId = _options.ApplicationId };
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.RsaSha256);

        var now = DateTime.UtcNow;
        var token = new JwtSecurityToken(
            issuer: "enablebanking.com",
            audience: "api.enablebanking.com",
            claims: null,
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: credentials);

        token.Header["kid"] = _options.ApplicationId;

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<EbAccountsResponse> GetAccountsAsync(string sessionId, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var sessionResponse = await _http.GetAsync($"/sessions/{sessionId}", ct);
        sessionResponse.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EbTransactionsResponse>(cancellationToken: ct);
        return result ?? new EbTransactionsResponse();
    }

    public async Task<List<EbAspspDto>> GetAspspsAsync(string country, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _http.GetAsync($"/aspsps?country={Uri.EscapeDataString(country)}", ct);
        response.EnsureSuccessStatusCode();
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
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EbAuthorizeResponse>(cancellationToken: ct);
        return result?.Url ?? throw new InvalidOperationException("Enable Banking não devolveu um URL de autorização.");
    }

    /// <summary>Troca o "code" recebido no callback por uma sessão válida com a lista de contas autorizadas.</summary>
    public async Task<EbSessionResponse> CreateSessionAsync(string authorizationCode, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _http.PostAsJsonAsync("/sessions", new { code = authorizationCode }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EbSessionResponse>(cancellationToken: ct);
        return result ?? new EbSessionResponse();
    }
}
