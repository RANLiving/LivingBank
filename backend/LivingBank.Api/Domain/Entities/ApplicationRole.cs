using Microsoft.AspNetCore.Identity;

namespace LivingBank.Api.Domain.Entities;

public class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = string.Empty;
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [Admin, Manager, Viewer];
}

public static class Permissions
{
    public const string ManageUsers = "permissions.manage_users";
    public const string ManageBankAccounts = "permissions.manage_bank_accounts";
    public const string ViewTransactions = "permissions.view_transactions";
    public const string ForceSync = "permissions.force_sync";
    public const string ViewLogs = "permissions.view_logs";
}
