using LivingBank.Api.Configuration;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Dtos;
using LivingBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LivingBank.Api.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController(
    AppDbContext db,
    ISyncService syncService,
    IOptions<EnableBankingOptions> ebOptions) : ControllerBase
{
    private readonly EnableBankingOptions _ebOptions = ebOptions.Value;

    /// <summary>Leitura forçada manual — bloqueia se a conta já atingiu o limite diário.</summary>
    [HttpPost("force/{bankAccountId}")]
    [Authorize(Policy = Permissions.ForceSync)]
    public async Task<ActionResult<SyncLogResponse>> ForceSync(Guid bankAccountId)
    {
        var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var todayCount = await syncService.GetTodaySyncCountAsync(bankAccountId);

        if (todayCount >= _ebOptions.MaxDailySyncsPerAccount)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, new
            {
                error = $"Limite diário de {_ebOptions.MaxDailySyncsPerAccount} leituras já atingido para esta conta.",
                todayCount
            });
        }

        var log = await syncService.SyncAccountAsync(bankAccountId, SyncTrigger.Manual, userId);
        return Ok(ToResponse(log));
    }

    [HttpGet("status/{bankAccountId}")]
    [Authorize]
    public async Task<ActionResult> GetStatus(Guid bankAccountId)
    {
        var todayCount = await syncService.GetTodaySyncCountAsync(bankAccountId);
        return Ok(new
        {
            todayCount,
            maxDaily = _ebOptions.MaxDailySyncsPerAccount,
            remaining = Math.Max(0, _ebOptions.MaxDailySyncsPerAccount - todayCount)
        });
    }

    [HttpGet("logs")]
    [Authorize(Policy = Permissions.ViewLogs)]
    public async Task<ActionResult<List<SyncLogResponse>>> GetLogs([FromQuery] Guid? bankAccountId, [FromQuery] int take = 100)
    {
        var query = db.SyncLogs.AsQueryable();
        if (bankAccountId.HasValue) query = query.Where(s => s.BankAccountId == bankAccountId.Value);

        var logs = await query.OrderByDescending(s => s.StartedAt).Take(take).ToListAsync();
        return Ok(logs.Select(ToResponse));
    }

    [HttpGet("schedule")]
    [Authorize]
    public async Task<ActionResult<SyncScheduleRequest>> GetSchedule()
    {
        var schedule = await db.SyncSchedules.FirstOrDefaultAsync() ?? new SyncSchedule();
        return Ok(new SyncScheduleRequest(schedule.Time1, schedule.Time2, schedule.Time3, schedule.Time4));
    }

    [HttpPut("schedule")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<ActionResult> UpdateSchedule(SyncScheduleRequest request)
    {
        var schedule = await db.SyncSchedules.FirstOrDefaultAsync();
        if (schedule is null)
        {
            schedule = new SyncSchedule { Id = 1 };
            db.SyncSchedules.Add(schedule);
        }
        schedule.Time1 = request.Time1;
        schedule.Time2 = request.Time2;
        schedule.Time3 = request.Time3;
        schedule.Time4 = request.Time4;
        schedule.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Endpoint chamado pelo GitHub Actions (cron externo) para acordar o serviço no Render
    /// e disparar a sincronização agendada, caso o Quartz interno não tenha corrido (serviço adormecido).
    /// Protegido por segredo partilhado em header, não por JWT de utilizador.
    /// </summary>
    [HttpPost("external-trigger")]
    [AllowAnonymous]
    public async Task<ActionResult> ExternalTrigger([FromHeader(Name = "X-Cron-Secret")] string? cronSecret)
    {
        if (string.IsNullOrEmpty(_ebOptions.ExternalCronSecret) || cronSecret != _ebOptions.ExternalCronSecret)
            return Unauthorized();

        var accounts = await db.BankAccounts.Where(a => a.IsActive).ToListAsync();
        var results = new List<object>();

        foreach (var account in accounts)
        {
            var todayCount = await syncService.GetTodaySyncCountAsync(account.Id);
            if (todayCount >= _ebOptions.MaxDailySyncsPerAccount)
            {
                results.Add(new { account.Id, skipped = true, reason = "limite diário atingido" });
                continue;
            }

            try
            {
                var log = await syncService.SyncAccountAsync(account.Id, SyncTrigger.Scheduled, null);
                results.Add(new { account.Id, success = true, log.BalancesFetched, log.TransactionsFetched });
            }
            catch (Exception ex)
            {
                results.Add(new { account.Id, success = false, error = ex.Message });
            }
        }

        return Ok(results);
    }

    private static SyncLogResponse ToResponse(SyncLog log) => new(
        log.Id, log.BankAccountId, log.Trigger.ToString(), log.Status.ToString(), log.ErrorMessage,
        log.BalancesFetched, log.TransactionsFetched, log.StartedAt, log.FinishedAt);
}
