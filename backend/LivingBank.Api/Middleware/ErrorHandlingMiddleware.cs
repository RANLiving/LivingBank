using System.Security.Claims;
using System.Text.Json;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Services;

namespace LivingBank.Api.Middleware;

/// <summary>
/// Captura globalmente exceções não tratadas, grava-as em ErrorLog e devolve uma resposta JSON uniforme.
/// </summary>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro não tratado em {Path}", context.Request.Path);

            var statusCode = ex switch
            {
                KeyNotFoundException => StatusCodes.Status404NotFound,
                DailyLimitExceededException => StatusCodes.Status429TooManyRequests,
                UnauthorizedAccessException => StatusCodes.Status403Forbidden,
                InvalidOperationException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            try
            {
                var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                db.ErrorLogs.Add(new ErrorLog
                {
                    Source = "Api",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace,
                    Path = context.Request.Path,
                    UserId = userIdClaim is not null ? Guid.Parse(userIdClaim) : null,
                    Timestamp = DateTimeOffset.UtcNow
                });
                await db.SaveChangesAsync();
            }
            catch (Exception logEx)
            {
                logger.LogError(logEx, "Falha ao gravar error log");
            }

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                error = ex.Message,
                statusCode
            }));
        }
    }
}
