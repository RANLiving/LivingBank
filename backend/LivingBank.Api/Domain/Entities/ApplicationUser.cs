using Microsoft.AspNetCore.Identity;

namespace LivingBank.Api.Domain.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<UserBankAccountAccess> BankAccountAccesses { get; set; } = new List<UserBankAccountAccess>();
}
