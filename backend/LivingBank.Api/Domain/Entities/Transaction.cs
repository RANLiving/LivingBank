namespace LivingBank.Api.Domain.Entities;

public class Transaction
{
    public long Id { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    // Identificador único do movimento devolvido pelo Enable Banking (entry_reference)
    public string ExternalId { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public string CreditDebitIndicator { get; set; } = "DBIT"; // DBIT ou CRDT
    public DateOnly BookingDate { get; set; }
    public DateOnly? ValueDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? CounterpartyName { get; set; }
    public string? CounterpartyIban { get; set; }
    public string Status { get; set; } = "booked"; // booked ou pending

    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;

    // Movimentos nunca são eliminados da base de dados — só marcados como exportados.
    public bool IsExported { get; set; } = false;
    public DateTimeOffset? ExportedAt { get; set; }
}
