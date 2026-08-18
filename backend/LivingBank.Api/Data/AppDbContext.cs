using LivingBank.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LivingBank.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<Balance> Balances => Set<Balance>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<UserBankAccountAccess> UserBankAccountAccesses => Set<UserBankAccountAccess>();
    public DbSet<SyncSchedule> SyncSchedules => Set<SyncSchedule>();
    public DbSet<Company> Companies => Set<Company>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<BankAccount>(e =>
        {
            e.HasIndex(x => x.EnableBankingAccountId).IsUnique();
            e.HasIndex(x => x.Iban);
            e.HasOne(x => x.Company).WithMany(c => c.BankAccounts).HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Company>(e =>
        {
            e.HasIndex(x => x.TaxId).IsUnique();
        });

        builder.Entity<Transaction>(e =>
        {
            e.HasIndex(x => new { x.BankAccountId, x.ExternalId }).IsUnique();
            e.Property(x => x.Amount).HasPrecision(18, 2);
        });

        builder.Entity<Balance>(e =>
        {
            e.Property(x => x.Amount).HasPrecision(18, 2);
            e.HasIndex(x => new { x.BankAccountId, x.ReferenceDate, x.BalanceType });
        });

        builder.Entity<SyncLog>(e =>
        {
            e.HasIndex(x => new { x.BankAccountId, x.SyncDate });
        });

        builder.Entity<UserBankAccountAccess>(e =>
        {
            e.HasKey(x => new { x.UserId, x.BankAccountId });
            e.HasOne(x => x.User).WithMany(u => u.BankAccountAccesses).HasForeignKey(x => x.UserId);
            e.HasOne(x => x.BankAccount).WithMany(a => a.UserAccesses).HasForeignKey(x => x.BankAccountId);
        });

        builder.Entity<AuditLog>(e =>
        {
            e.HasIndex(x => x.Timestamp);
        });

        builder.Entity<ErrorLog>(e =>
        {
            e.HasIndex(x => x.Timestamp);
        });

        builder.Entity<SyncSchedule>().HasData(new SyncSchedule
        {
            Id = 1,
            Time1 = new TimeOnly(6, 0),
            Time2 = new TimeOnly(12, 0),
            Time3 = new TimeOnly(18, 0),
            Time4 = new TimeOnly(23, 0),
            UpdatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
    }
}
