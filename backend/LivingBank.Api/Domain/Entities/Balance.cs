namespace LivingBank.Api.Domain.Entities;

public class Balance
{
    public long Id { get; set; }
    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public string BalanceType { get; set; } = "closingBooked";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EUR";
    public DateTimeOffset ReferenceDate { get; set; }
    public DateTimeOffset FetchedAt { get; set; } = DateTimeOffset.UtcNow;
}
