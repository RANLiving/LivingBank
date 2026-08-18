using LivingBank.Api.Domain.Entities;

namespace LivingBank.Api.Services;

public static class PasswordPolicy
{
    public static readonly TimeSpan MaxAge = TimeSpan.FromDays(60);

    /// <summary>Admin está isento da troca obrigatória de password a cada 60 dias.</summary>
    public static bool IsExpired(ApplicationUser user, IList<string> roles)
    {
        if (roles.Contains(Roles.Admin)) return false;
        return DateTimeOffset.UtcNow - user.PasswordChangedAt > MaxAge;
    }
}
