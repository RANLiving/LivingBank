using LivingBank.Api.Configuration;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Integrations.EnableBanking;
using LivingBank.Api.Services;
using LivingBank.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace LivingBank.Tests;

public class SyncServiceTests
{
    private readonly IEnableBankingClient _client = Substitute.For<IEnableBankingClient>();
    private readonly IAuditService _audit = Substitute.For<IAuditService>();
    private static readonly EnableBankingOptions Options = new() { MaxDailySyncsPerAccount = 4 };

    private SyncService Build(AppDbContext db) => new(
        db, _client, Microsoft.Extensions.Options.Options.Create(Options), _audit,
        Substitute.For<ILogger<SyncService>>());

    private static BankAccount SeedAccount(AppDbContext db, string? sessionId = "sess-1")
    {
        var account = new BankAccount
        {
            EnableBankingAccountId = "eb-acc-1",
            Iban = "PT50000201231234567890154",
            DisplayName = "Conta Principal",
            SessionId = sessionId,
            IsActive = true
        };
        db.BankAccounts.Add(account);
        db.SaveChanges();
        return account;
    }

    private static SyncLog Log(Guid accountId, SyncStatus status, DateOnly date, SyncTrigger trigger = SyncTrigger.Scheduled) => new()
    {
        BankAccountId = accountId,
        Status = status,
        SyncDate = date,
        Trigger = trigger,
        StartedAt = DateTimeOffset.UtcNow
    };

    private void SetupClient(EbBalancesResponse balances, params EbTransactionsResponse[] txPages)
    {
        _client.GetBalancesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(balances);
        _client.GetTransactionsAsync(Arg.Any<string>(), Arg.Any<DateOnly?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(txPages[0], txPages.Skip(1).ToArray());
    }

    private static EbBalancesResponse Balances(params (string type, string amount)[] items) => new()
    {
        Balances = items.Select(i => new EbBalanceDto
        {
            BalanceType = i.type,
            BalanceAmount = new EbAmountDto { Amount = i.amount, Currency = "EUR" },
            ReferenceDate = new DateOnly(2026, 2, 1)
        }).ToList()
    };

    private static EbTransactionsResponse Transactions(string? continuationKey, params (string extId, string amount, string cd)[] items) => new()
    {
        ContinuationKey = continuationKey,
        Transactions = items.Select(i => new EbTransactionDto
        {
            EntryReference = i.extId,
            TransactionAmount = new EbAmountDto { Amount = i.amount, Currency = "EUR" },
            CreditDebitIndicator = i.cd,
            BookingDate = new DateOnly(2026, 2, 10),
            Status = "BOOK"
        }).ToList()
    };

    // ---- GetTodaySyncCountAsync ----

    [Fact]
    public async Task GetTodaySyncCount_Counts_Only_Successful_Today_For_That_Account()
    {
        using var db = InMemoryDb.Create();
        var a = SeedAccount(db);
        var other = SeedAccount(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var yesterday = today.AddDays(-1);

        db.SyncLogs.AddRange(
            Log(a.Id, SyncStatus.Success, today),
            Log(a.Id, SyncStatus.Success, today),
            Log(a.Id, SyncStatus.Failure, today),      // não conta (falha)
            Log(a.Id, SyncStatus.Success, yesterday),  // não conta (outro dia)
            Log(other.Id, SyncStatus.Success, today));  // não conta (outra conta)
        db.SaveChanges();

        var count = await Build(db).GetTodaySyncCountAsync(a.Id);

        Assert.Equal(2, count);
    }

    // ---- Guard clauses ----

    [Fact]
    public async Task SyncAccount_Throws_When_Account_Missing()
    {
        using var db = InMemoryDb.Create();
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => Build(db).SyncAccountAsync(Guid.NewGuid(), SyncTrigger.Manual, null));
    }

    [Fact]
    public async Task SyncAccount_Throws_DailyLimitExceeded_When_At_Limit()
    {
        using var db = InMemoryDb.Create();
        var a = SeedAccount(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 4; i++) db.SyncLogs.Add(Log(a.Id, SyncStatus.Success, today));
        db.SaveChanges();

        await Assert.ThrowsAsync<DailyLimitExceededException>(
            () => Build(db).SyncAccountAsync(a.Id, SyncTrigger.Manual, null));

        await _client.DidNotReceiveWithAnyArgs().GetBalancesAsync(default!, default);
    }

    [Fact]
    public async Task SyncAccount_Without_Session_Persists_Failure_Log_And_Rethrows()
    {
        using var db = InMemoryDb.Create();
        var a = SeedAccount(db, sessionId: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => Build(db).SyncAccountAsync(a.Id, SyncTrigger.Scheduled, null));

        var log = Assert.Single(db.SyncLogs.ToList());
        Assert.Equal(SyncStatus.Failure, log.Status);
        Assert.False(string.IsNullOrWhiteSpace(log.ErrorMessage));
        Assert.NotNull(log.FinishedAt);
    }

    // ---- Success path ----

    [Fact]
    public async Task SyncAccount_Success_Persists_Balances_Transactions_And_Success_Log()
    {
        using var db = InMemoryDb.Create();
        var a = SeedAccount(db);
        SetupClient(
            Balances(("CLBD", "1500.50"), ("XPCD", "1490.00")),
            Transactions(null, ("ext-1", "100.00", "CRDT"), ("ext-2", "40.00", "DBIT")));

        var log = await Build(db).SyncAccountAsync(a.Id, SyncTrigger.Manual, null);

        Assert.Equal(SyncStatus.Success, log.Status);
        Assert.Equal(2, log.BalancesFetched);
        Assert.Equal(2, log.TransactionsFetched);
        Assert.NotNull(log.FinishedAt);

        Assert.Equal(2, db.Balances.Count(b => b.BankAccountId == a.Id));
        Assert.Equal(2, db.Transactions.Count(t => t.BankAccountId == a.Id));
        Assert.Equal(1500.50m, db.Balances.First(b => b.BalanceType == "CLBD").Amount);

        await _audit.Received(1).LogAsync(null, "Sync.Success", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAccount_Skips_Transactions_That_Already_Exist()
    {
        using var db = InMemoryDb.Create();
        var a = SeedAccount(db);
        db.Transactions.Add(new Transaction
        {
            BankAccountId = a.Id,
            ExternalId = "ext-1",
            Amount = 100m,
            CreditDebitIndicator = "CRDT",
            BookingDate = new DateOnly(2026, 2, 10)
        });
        db.SaveChanges();

        SetupClient(
            Balances(("CLBD", "10.00")),
            Transactions(null, ("ext-1", "100.00", "CRDT"), ("ext-2", "5.00", "CRDT")));

        var log = await Build(db).SyncAccountAsync(a.Id, SyncTrigger.Scheduled, null);

        Assert.Equal(1, log.TransactionsFetched); // só a ext-2 é nova
        Assert.Equal(2, db.Transactions.Count(t => t.BankAccountId == a.Id));
    }

    [Fact]
    public async Task SyncAccount_Follows_Continuation_Key_Across_Pages()
    {
        using var db = InMemoryDb.Create();
        var a = SeedAccount(db);
        SetupClient(
            Balances(("CLBD", "10.00")),
            Transactions("page-2", ("ext-1", "1.00", "CRDT")),
            Transactions(null, ("ext-2", "2.00", "CRDT")));

        var log = await Build(db).SyncAccountAsync(a.Id, SyncTrigger.Scheduled, null);

        Assert.Equal(2, log.TransactionsFetched);
        await _client.Received(2).GetTransactionsAsync(
            Arg.Any<string>(), Arg.Any<DateOnly?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SyncAccount_Persists_Failure_Log_When_Client_Throws()
    {
        using var db = InMemoryDb.Create();
        var a = SeedAccount(db);
        _client.GetBalancesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<EbBalancesResponse>(_ => throw new HttpRequestException("Enable Banking em baixo"));

        await Assert.ThrowsAsync<HttpRequestException>(
            () => Build(db).SyncAccountAsync(a.Id, SyncTrigger.Scheduled, null));

        var log = Assert.Single(db.SyncLogs.ToList());
        Assert.Equal(SyncStatus.Failure, log.Status);
        Assert.Contains("Enable Banking em baixo", log.ErrorMessage);
    }
}
