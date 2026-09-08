using System.Text;
using LivingBank.Api.Configuration;
using LivingBank.Api.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

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
    private const string JwtIssuer = "LivingBank";
    private const string JwtAudience = "LivingBank.Clients";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = JwtSecret,
                ["Jwt:Issuer"] = JwtIssuer,
                ["Jwt:Audience"] = JwtAudience,
                ["Seed:AdminUserName"] = AdminUserName,
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword,
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove tudo o que o Program.cs registou para o AppDbContext (incl. o provider Npgsql),
            // senão o EF Core recusa-se a arrancar com dois providers no mesmo service provider.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(_dbName));

            // O Program.cs lê Jwt:Secret uma vez, cedo, para configurar a validação do bearer.
            // Dependendo da ordem de aplicação das fontes de configuração nos testes, essa
            // leitura pode apanhar o segredo do appsettings.Development.json em vez do nosso.
            // Fixamos aqui a chave (assinatura e validação) para não haver mismatch.
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
            services.Configure<JwtOptions>(o =>
            {
                o.Secret = JwtSecret;
                o.Issuer = JwtIssuer;
                o.Audience = JwtAudience;
            });
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, o =>
            {
                o.TokenValidationParameters.IssuerSigningKey = key;
                o.TokenValidationParameters.ValidIssuer = JwtIssuer;
                o.TokenValidationParameters.ValidAudience = JwtAudience;
            });
        });
    }
}
