namespace LivingBank.Api.Middleware;

/// <summary>
/// Bloqueia o acesso ao resto da API quando o JWT tem a claim "pwd_expired"=true,
/// exceto nos endpoints necessários para o utilizador trocar a password e continuar.
/// </summary>
public class PasswordExpiryMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPaths =
    [
        "/api/auth/change-password",
        "/api/auth/me",
        "/api/auth/login"
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var expiredClaim = context.User.FindFirst("pwd_expired")?.Value;
        var isExpired = expiredClaim == "true";

        if (isExpired && !AllowedPaths.Contains(context.Request.Path.Value, StringComparer.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("""{"error":"A password expirou. É necessário trocá-la antes de continuar."}""");
            return;
        }

        await next(context);
    }
}
