using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;

namespace LivingBank.Api.Services;

public interface IAuditService
{
    Task LogAsync(Guid? userId, string action, string? details = null, CancellationToken ct = default);
}

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task LogAsync(Guid? userId, string action, string? details = null, CancellationToken ct = default)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            HttpMethod = "",
            Path = "",
            StatusCode = 0,
            Timestamp = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(ct);
    }
}
