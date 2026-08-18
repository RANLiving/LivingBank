namespace LivingBank.Api.Domain.Entities;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    // NIF / número de contribuinte
    public string TaxId { get; set; } = string.Empty;

    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<BankAccount> BankAccounts { get; set; } = new List<BankAccount>();
}
