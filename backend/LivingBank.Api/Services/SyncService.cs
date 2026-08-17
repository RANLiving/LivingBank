using LivingBank.Api.Configuration;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Integrations.EnableBanking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LivingBank.Api.Services;

public class DailyLimitExceededException(Guid bankAccountId, int limit)
    : Exception($"Conta {bankAccountId} já atingiu o limite de {limit} leituras hoje.");

public interface ISyncService
{
    Task<SyncLog> SyncAccountAsync(Guid bankAccountId, SyncTrigger trigger, Guid? triggeredByUserId, CancellationToken ct = default);
    Task<int> GetTodaySyncCountAsync(Guid bankAccountId, CancellationToken ct = default);
}

public class SyncService(
    AppDbContext db,
    IEnableBankingClient enableBankingClient,
    IOptions<EnableBankingOptions> options,
    IAuditService auditService,
    ILogger<SyncService> logger) : ISyncService
{
    private readonly EnableBankingOptions _options = options.Value;

    public async Task<int> GetTodaySyncCountAsync(Guid bankAccountId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return await db.SyncLogs.CountAsync(
            s => s.BankAccountId == bankAccountId && s.SyncDate == today && s.Status == SyncStatus.Success, ct);
    }

    public async Task<SyncLog> SyncAccountAsync(Guid bankAccountId, SyncTrigger trigger, Guid? triggeredByUserId, CancellationToken ct = default)
    {
        var account = await db.BankAccounts.FirstOrDefaultAsync(a => a.Id == bankAccountId, ct)
            ?? throw new KeyNotFoundException($"Conta bancária {bankAccountId} não encontrada.");

        var todayCount = await GetTodaySyncCountAsync(bankAccountId, ct);
        if (todayCount >= _options.MaxDailySyncsPerAccount)
        {
            throw new DailyLimitExceededException(bankAccountId, _options.MaxDailySyncsPerAccount);
        }

        var log = new SyncLog
        {
            BankAccountId = bankAccountId,
            Trigger = trigger,
            TriggeredByUserId = triggeredByUserId,
            SyncDate = DateOnly.FromDateTime(DateTime.UtcNow),
            StartedAt = DateTimeOffset.UtcNow
        };

        try
        {
            if (string.IsNullOrEmpty(account.SessionId))
                throw new InvalidOperationException("Conta sem sessão de consentimento Enable Banking ativa.");

            var balancesResponse = await enableBankingClient.GetBalancesAsync(account.EnableBankingAccountId, ct);
            foreach (var b in balancesResponse.Balances)
            {
                db.Balances.Add(new Balance
                {
                    BankAccountId = account.Id,
                    BalanceType = b.BalanceType,
                    Amount = decimal.Parse(b.BalanceAmount.Amount, System.Globalization.CultureInfo.InvariantCulture),
                    Currency = b.BalanceAmount.Currency,
                    ReferenceDate = b.ReferenceDate.HasValue
                        ? new DateTimeOffset(b.ReferenceDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero)
                        : DateTimeOffset.UtcNow
                });
            }
            log.BalancesFetched = balancesResponse.Balances.Count;

            string? continuationKey = null;
            var totalTransactions = 0;
            do
            {
                var txResponse = await enableBankingClient.GetTransactionsAsync(
                    account.EnableBankingAccountId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)), continuationKey, ct);

                foreach (var t in txResponse.Transactions)
                {
                    var exists = await db.Transactions.AnyAsync(
                        x => x.BankAccountId == account.Id && x.ExternalId == t.EntryReference, ct);
                    if (exists) continue;

                    db.Transactions.Add(new Transaction
                    {
                        BankAccountId = account.Id,
                        ExternalId = t.EntryReference,
                        Amount = decimal.Parse(t.TransactionAmount.Amount, System.Globalization.CultureInfo.InvariantCulture),
                        Currency = t.TransactionAmount.Currency,
                        CreditDebitIndicator = t.CreditDebitIndicator,
                        BookingDate = t.BookingDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
                        ValueDate = t.ValueDate,
                        Description = t.RemittanceInformation is { Count: > 0 } ? string.Join(" ", t.RemittanceInformation) : "",
                        CounterpartyName = t.Creditor?.Name ?? t.Debtor?.Name,
                        Status = t.Status
                    });
                    totalTransactions++;
                }

                continuationKey = txResponse.ContinuationKey;
            } while (!string.IsNullOrEmpty(continuationKey));

            log.TransactionsFetched = totalTransactions;
            log.Status = SyncStatus.Success;
            log.FinishedAt = DateTimeOffset.UtcNow;

            db.SyncLogs.Add(log);
            await db.SaveChangesAsync(ct);

            await auditService.LogAsync(triggeredByUserId, "Sync.Success",
                $"Conta {account.DisplayName}: {log.BalancesFetched} saldos, {log.TransactionsFetched} movimentos.", ct);

            return log;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao sincronizar conta {AccountId}", bankAccountId);
            log.Status = SyncStatus.Failure;
            log.ErrorMessage = ex.Message;
            log.FinishedAt = DateTimeOffset.UtcNow;
            db.SyncLogs.Add(log);
            await db.SaveChangesAsync(ct);
            throw;
        }
    }
}
