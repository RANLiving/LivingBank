using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Dtos;
using LivingBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivingBank.Api.Controllers;

[ApiController]
[Route("api/bank-accounts")]
[Authorize]
public class BankAccountsController(AppDbContext db, IAuditService auditService) : ControllerBase
{
    // Prioridade de tipos de saldo do Enable Banking (ISO 20022) para escolher qual mostrar
    // como "saldo atual" — closing booked é o saldo contabilístico fechado do dia anterior,
    // preferimos interim available (saldo disponível em tempo real) quando existir.
    private static readonly string[] BalanceTypePriority = ["interimAvailable", "ITAV", "closingBooked", "CLBD", "expected", "XPCD", "openingBooked", "OPBD"];

    [HttpGet]
    public async Task<ActionResult<List<BankAccountResponse>>> GetAll()
    {
        var accounts = await db.BankAccounts
            .Select(a => new
            {
                Account = a,
                LatestBalance = a.Balances
                    .OrderByDescending(b => b.FetchedAt)
                    .Select(b => new { b.Amount, b.BalanceType, b.FetchedAt })
                    .ToList(),
                TransactionCount = a.Transactions.Count()
            })
            .ToListAsync();

        var result = accounts.Select(x =>
        {
            var picked = BalanceTypePriority
                .Select(type => x.LatestBalance.FirstOrDefault(b => b.BalanceType == type))
                .FirstOrDefault(b => b is not null)
                ?? x.LatestBalance.FirstOrDefault();

            return new BankAccountResponse(
                x.Account.Id, x.Account.Iban, x.Account.BankName, x.Account.DisplayName, x.Account.Currency,
                x.Account.IsActive, x.Account.ConsentValidUntil,
                picked is not null ? (decimal?)picked.Amount : null,
                picked is not null ? (DateTimeOffset?)picked.FetchedAt : null,
                x.TransactionCount);
        }).ToList();

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<ActionResult<BankAccountResponse>> Create(CreateBankAccountRequest request)
    {
        var account = new BankAccount
        {
            EnableBankingAccountId = request.EnableBankingAccountId,
            Iban = request.Iban,
            BankName = request.BankName,
            DisplayName = request.DisplayName,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "EUR" : request.Currency,
            AspspName = request.AspspName,
            AspspCountry = string.IsNullOrWhiteSpace(request.AspspCountry) ? "PT" : request.AspspCountry,
            SessionId = request.SessionId,
            ConsentValidUntil = request.ConsentValidUntil
        };
        db.BankAccounts.Add(account);
        await db.SaveChangesAsync();

        await auditService.LogAsync(GetCurrentUserId(), "BankAccount.Create", $"conta={account.DisplayName} iban={account.Iban}");
        return Ok(new BankAccountResponse(account.Id, account.Iban, account.BankName, account.DisplayName, account.Currency, account.IsActive, account.ConsentValidUntil, null, null, 0));
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var account = await db.BankAccounts.FindAsync(id);
        if (account is null) return NotFound();
        account.IsActive = false;
        await db.SaveChangesAsync();
        await auditService.LogAsync(GetCurrentUserId(), "BankAccount.Deactivate", $"conta={account.DisplayName}");
        return NoContent();
    }

    [HttpGet("{id}/balances")]
    public async Task<ActionResult<List<BalanceResponse>>> GetBalances(Guid id, [FromQuery] int take = 30)
    {
        var balances = await db.Balances
            .Where(b => b.BankAccountId == id)
            .OrderByDescending(b => b.FetchedAt)
            .Take(take)
            .Select(b => new BalanceResponse(b.Id, b.BalanceType, b.Amount, b.Currency, b.ReferenceDate, b.FetchedAt))
            .ToListAsync();
        return Ok(balances);
    }

    [HttpGet("{id}/transactions")]
    public async Task<ActionResult<List<TransactionResponse>>> GetTransactions(
        Guid id, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var query = db.Transactions.Where(t => t.BankAccountId == id);
        if (from.HasValue) query = query.Where(t => t.BookingDate >= from.Value);
        if (to.HasValue) query = query.Where(t => t.BookingDate <= to.Value);

        var transactions = await query
            .OrderByDescending(t => t.BookingDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionResponse(t.Id, t.Amount, t.Currency, t.CreditDebitIndicator, t.BookingDate, t.ValueDate, t.Description, t.CounterpartyName, t.Status))
            .ToListAsync();

        return Ok(transactions);
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? Guid.Parse(claim) : null;
    }
}
