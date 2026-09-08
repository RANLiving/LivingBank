using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Jobs;
using LivingBank.Api.Services;
using LivingBank.Tests.TestSupport;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Quartz;

namespace LivingBank.Tests;

public class ScheduledSyncJobTests
{
    private readonly ISyncService _sync = Substitute.For<ISyncService>();

    private ScheduledSyncJob Build(AppDbContext db) =>
        new(db, _sync, Substitute.For<ILogger<ScheduledSyncJob>>());

    private static IJobExecutionContext Context()
    {
        var ctx = Substitute.For<IJobExecutionContext>();
        ctx.CancellationToken.Returns(CancellationToken.None);
        return ctx;
    }

    /// <summary>Todos os horários a 00:00 UTC — garante que os 4 slots já "passaram" a qualquer hora do dia.</summary>
    private static void SeedScheduleAllSlotsPassed(AppDbContext db)
    {
        var s = db.SyncSchedules.FirstOrDefault();
        if (s is null) { s = new SyncSchedule { Id = 1 }; db.SyncSchedules.Add(s); }
        s.Time1 = s.Time2 = s.Time3 = s.Time4 = new TimeOnly(0, 0, 0);
        db.SaveChanges();
    }

    private static void SeedScheduleNoSlotsPassed(AppDbContext db)
    {
        var s = db.SyncSchedules.FirstOrDefault();
        if (s is null) { s = new SyncSchedule { Id = 1 }; db.SyncSchedules.Add(s); }
        s.Time1 = s.Time2 = s.Time3 = s.Time4 = new TimeOnly(23, 59, 59);
        db.SaveChanges();
    }

    private static BankAccount SeedAccount(AppDbContext db, bool active = true)
    {
        var a = new BankAccount { EnableBankingAccountId = Guid.NewGuid().ToString(), IsActive = active, DisplayName = "C" };
        db.BankAccounts.Add(a);
        db.SaveChanges();
        return a;
    }

    private static void SeedScheduledSuccesses(AppDbContext db, Guid accountId, int count)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < count; i++)
            db.SyncLogs.Add(new SyncLog
            {
                BankAccountId = accountId,
                Status = SyncStatus.Success,
                Trigger = SyncTrigger.Scheduled,
                SyncDate = today,
                StartedAt = DateTimeOffset.UtcNow
            });
        db.SaveChanges();
    }

    [Fact]
    public async Task Does_Nothing_When_No_Slots_Passed_Yet()
    {
        using var db = InMemoryDb.Create();
        SeedScheduleNoSlotsPassed(db);
        SeedAccount(db);

        await Build(db).Execute(Context());

        await _sync.DidNotReceiveWithAnyArgs().SyncAccountAsync(default, default, default, default);
    }

    [Fact]
    public async Task Does_Nothing_When_There_Are_No_Active_Accounts()
    {
        using var db = InMemoryDb.Create();
        SeedScheduleAllSlotsPassed(db);
        SeedAccount(db, active: false);

        await Build(db).Execute(Context());

        await _sync.DidNotReceiveWithAnyArgs().SyncAccountAsync(default, default, default, default);
    }

    [Fact]
    public async Task Triggers_Scheduled_Sync_For_Account_With_No_Runs_Today()
    {
        using var db = InMemoryDb.Create();
        SeedScheduleAllSlotsPassed(db);
        var a = SeedAccount(db);

        await Build(db).Execute(Context());

        await _sync.Received(1).SyncAccountAsync(a.Id, SyncTrigger.Scheduled, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Skips_Account_That_Already_Caught_Up_With_Passed_Slots()
    {
        using var db = InMemoryDb.Create();
        SeedScheduleAllSlotsPassed(db); // 4 slots passados
        var a = SeedAccount(db);
        SeedScheduledSuccesses(db, a.Id, 4);

        await Build(db).Execute(Context());

        await _sync.DidNotReceiveWithAnyArgs().SyncAccountAsync(default, default, default, default);
    }

    [Fact]
    public async Task Runs_Once_For_Account_That_Is_Behind_On_Passed_Slots()
    {
        using var db = InMemoryDb.Create();
        SeedScheduleAllSlotsPassed(db); // 4 slots passados
        var a = SeedAccount(db);
        SeedScheduledSuccesses(db, a.Id, 2); // só 2 de 4 feitas

        await Build(db).Execute(Context());

        await _sync.Received(1).SyncAccountAsync(a.Id, SyncTrigger.Scheduled, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Manual_Syncs_Today_Do_Not_Count_Towards_Scheduled_Quota()
    {
        using var db = InMemoryDb.Create();
        SeedScheduleAllSlotsPassed(db);
        var a = SeedAccount(db);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 4; i++)
            db.SyncLogs.Add(new SyncLog
            {
                BankAccountId = a.Id,
                Status = SyncStatus.Success,
                Trigger = SyncTrigger.Manual,
                SyncDate = today,
                StartedAt = DateTimeOffset.UtcNow
            });
        db.SaveChanges();

        await Build(db).Execute(Context());

        await _sync.Received(1).SyncAccountAsync(a.Id, SyncTrigger.Scheduled, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task One_Failing_Account_Does_Not_Stop_The_Others()
    {
        using var db = InMemoryDb.Create();
        SeedScheduleAllSlotsPassed(db);
        var bad = SeedAccount(db);
        var good = SeedAccount(db);

        _sync.SyncAccountAsync(bad.Id, Arg.Any<SyncTrigger>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns<SyncLog>(_ => throw new InvalidOperationException("boom"));

        await Build(db).Execute(Context());

        await _sync.Received(1).SyncAccountAsync(good.Id, SyncTrigger.Scheduled, null, Arg.Any<CancellationToken>());
    }
}
