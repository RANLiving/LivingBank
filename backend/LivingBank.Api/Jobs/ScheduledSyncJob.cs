using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace LivingBank.Api.Jobs;

/// <summary>
/// Corre a cada 5 minutos (ver Program.cs). Compara a hora atual (UTC) com os 4 horários
/// configurados em SyncSchedule; se estiver dentro da janela e ainda não houver uma sincronização
/// agendada bem-sucedida para essa conta/horário hoje, dispara a leitura ao Enable Banking.
/// </summary>
[DisallowConcurrentExecution]
public class ScheduledSyncJob(AppDbContext db, ISyncService syncService, ILogger<ScheduledSyncJob> logger) : IJob
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(5);

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var schedule = await db.SyncSchedules.FirstOrDefaultAsync(ct) ?? new SyncSchedule();
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);

        var dueSlot = schedule.Times.FirstOrDefault(t => IsWithinWindow(now, t));
        if (dueSlot == default) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var accounts = await db.BankAccounts.Where(a => a.IsActive).ToListAsync(ct);

        foreach (var account in accounts)
        {
            var alreadyRanThisSlot = await db.SyncLogs.AnyAsync(s =>
                s.BankAccountId == account.Id &&
                s.SyncDate == today &&
                s.Trigger == SyncTrigger.Scheduled &&
                s.StartedAt.TimeOfDay >= dueSlot.AddMinutes(-Window.TotalMinutes).ToTimeSpan() &&
                s.StartedAt.TimeOfDay <= dueSlot.AddMinutes(Window.TotalMinutes).ToTimeSpan(), ct);

            if (alreadyRanThisSlot) continue;

            try
            {
                await syncService.SyncAccountAsync(account.Id, SyncTrigger.Scheduled, null, ct);
                logger.LogInformation("Sincronização agendada concluída para conta {AccountId}", account.Id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na sincronização agendada da conta {AccountId}", account.Id);
            }
        }
    }

    private static bool IsWithinWindow(TimeOnly now, TimeOnly target)
    {
        var diff = Math.Abs((now.ToTimeSpan() - target.ToTimeSpan()).TotalMinutes);
        return diff <= Window.TotalMinutes;
    }
}
