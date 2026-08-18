using LivingBank.Api.Configuration;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Dtos;
using LivingBank.Api.Integrations.EnableBanking;
using LivingBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LivingBank.Api.Controllers;

/// <summary>
/// Fluxo de consentimento PSD2 do Enable Banking: escolher banco → autorizar no banco →
/// voltar com uma sessão válida → escolher quais contas dessa sessão gravar na LivingBank.
/// </summary>
[ApiController]
[Route("api/bank-link")]
public class BankLinkController(
    IEnableBankingClient enableBankingClient,
    IOptions<EnableBankingOptions> options,
    AppDbContext db,
    IAuditService auditService,
    IMemoryCache cache) : ControllerBase
{
    private readonly EnableBankingOptions _options = options.Value;
    private static string CacheKey(string sessionId) => $"eb-session-accounts:{sessionId}";

    [HttpGet("aspsps")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<ActionResult<List<AspspOption>>> GetAspsps([FromQuery] string country = "PT")
    {
        var aspsps = await enableBankingClient.GetAspspsAsync(country);
        return Ok(aspsps.Select(a => new AspspOption(a.Name, a.Country, a.Logo)));
    }

    [HttpPost("authorize")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<ActionResult<StartLinkResponse>> Authorize(StartLinkRequest request)
    {
        if (string.IsNullOrWhiteSpace(_options.RedirectUrl))
            return BadRequest(new { error = "EnableBanking:RedirectUrl não está configurado no backend." });

        var state = Guid.NewGuid().ToString("N");
        var url = await enableBankingClient.StartAuthorizationAsync(
            request.AspspName, request.AspspCountry, _options.RedirectUrl, state, validDays: 90);

        return Ok(new StartLinkResponse(url));
    }

    /// <summary>
    /// O ASPSP redireciona o browser do utilizador para aqui após o consentimento.
    /// Troca o "code" pela sessão e reencaminha para o frontend com o session_id.
    /// </summary>
    [HttpGet("callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string? state)
    {
        if (string.IsNullOrWhiteSpace(_options.FrontendCallbackUrl))
            return BadRequest(new { error = "EnableBanking:FrontendCallbackUrl não está configurado no backend." });

        try
        {
            var session = await enableBankingClient.CreateSessionAsync(code);
            cache.Set(CacheKey(session.SessionId), session.Accounts, TimeSpan.FromMinutes(30));

            var target = $"{_options.FrontendCallbackUrl}?sessionId={Uri.EscapeDataString(session.SessionId)}&state={Uri.EscapeDataString(state ?? "")}";
            return Redirect(target);
        }
        catch (Exception ex)
        {
            db.ErrorLogs.Add(new Domain.Entities.ErrorLog
            {
                Source = "BankLink.Callback",
                Message = ex.Message,
                Path = "/api/bank-link/callback"
            });
            await db.SaveChangesAsync();
            return Redirect($"{_options.FrontendCallbackUrl}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpGet("session/{sessionId}/accounts")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<ActionResult<SessionAccountsResponse>> GetSessionAccounts(string sessionId)
    {
        List<Integrations.EnableBanking.EbAccountDto> accountDtos;
        if (cache.TryGetValue(CacheKey(sessionId), out List<Integrations.EnableBanking.EbAccountDto>? cached) && cached is not null)
        {
            accountDtos = cached;
        }
        else
        {
            var accounts = await enableBankingClient.GetAccountsAsync(sessionId);
            accountDtos = accounts.Accounts;
        }

        var options = accountDtos.Select(a => new LinkedAccountOption(
            a.Uid, a.AccountId?.Iban, a.Name ?? a.Product, a.Currency));
        return Ok(new SessionAccountsResponse(sessionId, options.ToList()));
    }

    [HttpPost("save")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<ActionResult<BankAccountResponse>> SaveLinkedAccount(SaveLinkedAccountRequest request)
    {
        var exists = await db.BankAccounts.AnyAsync(a => a.EnableBankingAccountId == request.AccountUid);
        if (exists)
            return Conflict(new { error = "Esta conta já está ligada." });

        var account = new Domain.Entities.BankAccount
        {
            EnableBankingAccountId = request.AccountUid,
            Iban = request.Iban,
            BankName = request.BankName,
            DisplayName = request.DisplayName,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            AspspName = request.BankName,
            SessionId = request.SessionId,
            ConsentValidUntil = DateTimeOffset.UtcNow.AddDays(90)
        };
        db.BankAccounts.Add(account);
        await db.SaveChangesAsync();

        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        await auditService.LogAsync(
            userIdClaim is not null ? Guid.Parse(userIdClaim) : null,
            "BankAccount.Link", $"conta={account.DisplayName} iban={account.Iban} banco={account.BankName}");

        return Ok(new BankAccountResponse(account.Id, account.Iban, account.BankName, account.DisplayName, account.Currency, account.IsActive, account.ConsentValidUntil, null, null));
    }
}
