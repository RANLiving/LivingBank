using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Services;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace LivingBank.Api.Jobs;

/// <summary>
/// Corre a cada 5 minutos (ver Program.cs). Auto-recuperável: em vez de comparar a hora atual
/// com uma janela estreita à volta de cada horário configurado (o que perdia leituras sempre
/// que o serviço estava a dormir exatamente nesse minuto, ou o cron externo do GitHub Actions
/// atrasava — documentado e comum), conta quantos dos 4 horários já passaram hoje e quantas
/// sincronizações agendadas com sucesso já aconteceram — se faltar alguma, dispara uma agora.
/// Assim recupera sozinho sempre que o serviço acorda, independentemente de quando.
/// </summary>
[DisallowConcurrentExecution]
public class ScheduledSyncJob(AppDbContext db, ISyncService syncService, ILogger<ScheduledSyncJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var schedule = await db.SyncSchedules.FirstOrDefaultAsync(ct) ?? new SyncSchedule();
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var passedSlotsCount = schedule.Times.Count(t => t <= now);
        if (passedSlotsCount == 0) return;

        var accounts = await db.BankAccounts.Where(a => a.IsActive).ToListAsync(ct);

        foreach (var account in accounts)
        {
            var successfulScheduledToday = await db.SyncLogs.CountAsync(s =>
                s.BankAccountId == account.Id &&
                s.SyncDate == today &&
                s.Trigger == SyncTrigger.Scheduled &&
                s.Status == SyncStatus.Success, ct);

            if (successfulScheduledToday >= passedSlotsCount) continue;

            try
            {
                await syncService.SyncAccountAsync(account.Id, SyncTrigger.Scheduled, null, ct);
                logger.LogInformation("Sincronização agendada concluída para conta {AccountId} ({Done}/{Passed} horários de hoje)", account.Id, successfulScheduledToday + 1, passedSlotsCount);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha na sincronização agendada da conta {AccountId}", account.Id);
            }
        }
    }
}
