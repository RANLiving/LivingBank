namespace LivingBank.Api.Domain.Entities;

/// <summary>
/// Linha única de configuração com as 4 horas diárias (UTC) em que o cron lê o Enable Banking.
/// </summary>
public class SyncSchedule
{
    public int Id { get; set; } = 1;
    public TimeOnly Time1 { get; set; } = new TimeOnly(6, 0);
    public TimeOnly Time2 { get; set; } = new TimeOnly(12, 0);
    public TimeOnly Time3 { get; set; } = new TimeOnly(18, 0);
    public TimeOnly Time4 { get; set; } = new TimeOnly(23, 0);
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public TimeOnly[] Times => [Time1, Time2, Time3, Time4];
}
