namespace LivingBank.Api.Domain.Entities;

public class AuditLog
{
    public long Id { get; set; }
    public Guid? UserId { get; set; }
    public string? UserName { get; set; }

    public string Action { get; set; } = string.Empty; // ex: "BankAccount.Create", "Auth.Login"
    public string HttpMethod { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public string? Details { get; set; } // JSON livre com contexto adicional

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
