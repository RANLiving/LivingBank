using Microsoft.AspNetCore.Identity;

namespace LivingBank.Api.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    // Usado para forçar troca de password a cada 60 dias (exceto utilizadores com role Admin).
    public DateTimeOffset PasswordChangedAt { get; set; } = DateTimeOffset.UtcNow;

    // Falso enquanto o utilizador não definir a própria password através do link enviado
    // por email (convite inicial ou reenvio forçado) — nesse estado não consegue entrar.
    public bool PasswordSet { get; set; } = true;

    public ICollection<UserBankAccountAccess> BankAccountAccesses { get; set; } = new List<UserBankAccountAccess>();
}
