using LivingBank.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LivingBank.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        foreach (var roleName in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName, Description = $"Role {roleName}" });
            }
        }

        if (userManager.Users.Any()) return;

        var adminUserName = config["Seed:AdminUserName"];
        var adminEmail = config["Seed:AdminEmail"];
        var adminPassword = config["Seed:AdminPassword"];

        if (string.IsNullOrWhiteSpace(adminUserName) || string.IsNullOrWhiteSpace(adminPassword) || string.IsNullOrWhiteSpace(adminEmail))
        {
            // Sem utilizadores e sem credenciais de seed configuradas: não cria admin automaticamente.
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            FullName = "Administrador LivingBank",
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, Roles.Admin);
        }
    }
}
