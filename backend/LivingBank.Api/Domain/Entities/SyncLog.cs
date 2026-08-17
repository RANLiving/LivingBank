namespace LivingBank.Api.Domain.Entities;

public enum SyncTrigger
{
    Scheduled,
    Manual
}

public enum SyncStatus
{
    Success,
    Failure
}

public class SyncLog
{
    public long Id { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public SyncTrigger Trigger { get; set; }
    public SyncStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public int BalancesFetched { get; set; }
    public int TransactionsFetched { get; set; }

    // Utilizador que despoletou uma leitura manual (nulo se agendada)
    public Guid? TriggeredByUserId { get; set; }

    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? FinishedAt { get; set; }

    // Dia (UTC) usado para contar o limite de 4 leituras diárias por conta
    public DateOnly SyncDate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow);
}
