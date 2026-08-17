namespace LivingBank.Api.Domain.Entities;

public class ErrorLog
{
    public long Id { get; set; }
    public string Source { get; set; } = string.Empty; // ex: "EnableBankingSync", "Api"
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public string? Path { get; set; }
    public Guid? UserId { get; set; }
    public bool Resolved { get; set; } = false;

    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}
