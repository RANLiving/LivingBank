using LivingBank.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LivingBank.Tests.TestSupport;

/// <summary>
/// Fábrica de <see cref="AppDbContext"/> para testes. Usa o provider InMemory do EF Core:
/// não valida constraints relacionais nem índices únicos, mas é suficiente para exercitar
/// a lógica dos serviços (queries LINQ, contagens, projeções) sem uma base PostgreSQL real.
/// </summary>
public static class InMemoryDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"livingbank-tests-{Guid.NewGuid()}")
            .EnableSensitiveDataLogging()
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }
}
