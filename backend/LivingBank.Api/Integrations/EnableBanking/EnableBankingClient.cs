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
            claims: [new System.Security.Claims.Claim("iss", _options.ApplicationId)],
            notBefore: now,
            expires: now.AddMinutes(5),
            signingCredentials: credentials);

        token.Header["kid"] = _options.ApplicationId;

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<EbAccountsResponse> GetAccountsAsync(string sessionId, CancellationToken ct = default)
    {
        ApplyAuthHeader();
        var response = await _http.GetAsync($"/sessions/{sessionId}", ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EbAccountsResponse>(cancellationToken: ct);
        return result ?? new EbAccountsResponse();
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
}
