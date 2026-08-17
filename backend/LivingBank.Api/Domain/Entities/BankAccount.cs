namespace LivingBank.Api.Domain.Entities;

public class BankAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Identificador da conta no Enable Banking (account UID devolvido pela sessão)
    public string EnableBankingAccountId { get; set; } = string.Empty;

    public string Iban { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Currency { get; set; } = "EUR";

    // Referência ASPSP exigida pelo Enable Banking (código do banco no país)
    public string AspspName { get; set; } = string.Empty;
    public string AspspCountry { get; set; } = "PT";

    // Sessão de consentimento ativa (Enable Banking session_id) e validade
    public string? SessionId { get; set; }
    public DateTimeOffset? ConsentValidUntil { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Balance> Balances { get; set; } = new List<Balance>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();
    public ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();
    public ICollection<UserBankAccountAccess> UserAccesses { get; set; } = new List<UserBankAccountAccess>();
}
