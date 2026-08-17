namespace LivingBank.Api.Domain.Entities;

public class UserBankAccountAccess
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public Guid BankAccountId { get; set; }
    public BankAccount BankAccount { get; set; } = null!;

    public bool CanViewBalances { get; set; } = true;
    public bool CanViewTransactions { get; set; } = true;
}
