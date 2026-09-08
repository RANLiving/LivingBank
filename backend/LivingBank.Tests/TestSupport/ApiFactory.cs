using LivingBank.Api.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LivingBank.Tests.TestSupport;

/// <summary>
/// Sobe a API real em memória (pipeline, auth, controllers, seeding), trocando apenas o
/// PostgreSQL pelo provider InMemory do EF Core. Cada instância usa uma base isolada.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"livingbank-api-tests-{Guid.NewGuid()}";

    public const string AdminUserName = "admin";
    public const string AdminEmail = "admin@livingbank.test";
    public const string AdminPassword = "Str0ng!Passw0rd";

    private const string JwtSecret = "integration-tests-secret-key-0123456789-abcdefghijklmnopqrstuvwxyz";

    static ApiFactory()
    {
        // O Program.cs lê Jwt:Secret e Seed:* muito cedo (antes de qualquer customização
        // da WebApplicationFactory), por isso a configuração tem de vir do ambiente do
        // processo. O appsettings.Development.json não está no repositório, logo no CI
        // estas chaves ficariam vazias e o SymmetricSecurityKey rebentava.
        SetIfMissing("Jwt__Secret", JwtSecret);
        SetIfMissing("Jwt__Issuer", "LivingBank");
        SetIfMissing("Jwt__Audience", "LivingBank.Clients");
        SetIfMissing("Seed__AdminUserName", AdminUserName);
        SetIfMissing("Seed__AdminEmail", AdminEmail);
        SetIfMissing("Seed__AdminPassword", AdminPassword);
    }

    private static void SetIfMissing(string name, string value)
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
            Environment.SetEnvironmentVariable(name, value);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // Remove tudo o que o Program.cs registou para o AppDbContext (incl. o provider Npgsql),
            // senão o EF Core recusa-se a arrancar com dois providers no mesmo service provider.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));
        });
    }
}
