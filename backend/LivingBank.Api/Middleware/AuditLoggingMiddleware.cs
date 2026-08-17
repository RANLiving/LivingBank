using System.Security.Claims;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;

namespace LivingBank.Api.Middleware;

/// <summary>
/// Regista todas as operações de escrita (POST/PUT/PATCH/DELETE) efetuadas na plataforma.
/// </summary>
public class AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
{
    private static readonly string[] AuditedMethods = ["POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        await next(context);

        if (!AuditedMethods.Contains(context.Request.Method)) return;
        if (context.Request.Path.StartsWithSegments("/api/auth/login")) return;

        try
        {
            var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = context.User.FindFirstValue(ClaimTypes.Name);

            db.AuditLogs.Add(new AuditLog
            {
                UserId = userIdClaim is not null ? Guid.Parse(userIdClaim) : null,
                UserName = userName,
                Action = $"{context.Request.Method} {context.Request.Path}",
                HttpMethod = context.Request.Method,
                Path = context.Request.Path,
                StatusCode = context.Response.StatusCode,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                Timestamp = DateTimeOffset.UtcNow
            });
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao gravar audit log");
        }
    }
}
