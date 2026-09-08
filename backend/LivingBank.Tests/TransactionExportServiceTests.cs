using ClosedXML.Excel;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Dtos;
using LivingBank.Api.Services;
using LivingBank.Tests.TestSupport;

namespace LivingBank.Tests;

public class TransactionExportServiceTests
{
    // ---- ResolvePeriod (lógica de datas pura) ----

    [Fact]
    public void ResolvePeriod_Custom_Returns_Given_Range()
    {
        var from = new DateOnly(2026, 3, 1);
        var to = new DateOnly(2026, 3, 31);

        var (rf, rt) = TransactionExportService.ResolvePeriod(ExportPeriod.Custom, from, to);

        Assert.Equal(from, rf);
        Assert.Equal(to, rt);
    }

    [Fact]
    public void ResolvePeriod_Custom_Without_Dates_Throws()
    {
        Assert.Throws<InvalidOperationException>(
            () => TransactionExportService.ResolvePeriod(ExportPeriod.Custom, null, null));
        Assert.Throws<InvalidOperationException>(
            () => TransactionExportService.ResolvePeriod(ExportPeriod.Custom, new DateOnly(2026, 1, 1), null));
    }

    [Fact]
    public void ResolvePeriod_PreviousMonth_Is_Whole_Prior_Month()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);

        var (from, to) = TransactionExportService.ResolvePeriod(ExportPeriod.PreviousMonth, null, null);

        Assert.Equal(1, from.Day);
        Assert.Equal(firstOfThisMonth.AddMonths(-1), from);
        Assert.Equal(firstOfThisMonth.AddDays(-1), to);
        Assert.True(to < firstOfThisMonth);
    }

    [Fact]
    public void ResolvePeriod_PreviousQuarter_Spans_Three_Months_Starting_On_Quarter_Boundary()
    {
        var (from, to) = TransactionExportService.ResolvePeriod(ExportPeriod.PreviousQuarter, null, null);

        Assert.Contains(from.Month, new[] { 1, 4, 7, 10 });
        Assert.Equal(1, from.Day);
        Assert.Equal(from.AddMonths(3).AddDays(-1), to);
    }

    [Fact]
    public void ResolvePeriod_PreviousSemester_Starts_In_January_Or_July()
    {
        var (from, to) = TransactionExportService.ResolvePeriod(ExportPeriod.PreviousSemester, null, null);

        Assert.Contains(from.Month, new[] { 1, 7 });
        Assert.Equal(1, from.Day);
        Assert.Equal(from.AddMonths(6).AddDays(-1), to);
    }

    [Fact]
    public void ResolvePeriod_CurrentYear_Is_Jan1_To_Today()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var (from, to) = TransactionExportService.ResolvePeriod(ExportPeriod.CurrentYear, null, null);

        Assert.Equal(new DateOnly(today.Year, 1, 1), from);
        Assert.Equal(today, to);
    }

    // ---- ExportAsync (integração com AppDbContext InMemory + ClosedXML) ----

    private static BankAccount SeedAccount(LivingBank.Api.Data.AppDbContext db)
    {
        var account = new BankAccount
        {
            Iban = "PT50000201231234567890154",
            DisplayName = "Conta Principal",
            BankName = "Banco Teste"
        };
        db.BankAccounts.Add(account);
        db.SaveChanges();
        return account;
    }

    private static Transaction Tx(Guid accountId, DateOnly date, decimal amount, string indicator, bool exported = false) => new()
    {
        BankAccountId = accountId,
        ExternalId = Guid.NewGuid().ToString(),
        Amount = amount,
        Currency = "EUR",
        CreditDebitIndicator = indicator,
        BookingDate = date,
        Description = "Movimento teste",
        Status = "booked",
        IsExported = exported
    };

    [Fact]
    public async Task ExportAsync_Throws_When_Account_Missing()
    {
        using var db = InMemoryDb.Create();
        var service = new TransactionExportService(db);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ExportAsync(
            Guid.NewGuid(),
            new ExportTransactionsRequest(ExportScope.All, ExportPeriod.Custom, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))));
    }

    [Fact]
    public async Task ExportAsync_Throws_When_No_Transactions_In_Range()
    {
        using var db = InMemoryDb.Create();
        var account = SeedAccount(db);
        db.Transactions.Add(Tx(account.Id, new DateOnly(2020, 1, 1), 10m, "CRDT"));
        db.SaveChanges();

        var service = new TransactionExportService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(
            account.Id,
            new ExportTransactionsRequest(ExportScope.All, ExportPeriod.Custom, new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31))));
    }

    [Fact]
    public async Task ExportAsync_Produces_Workbook_Marks_Rows_Exported_And_Names_File_By_Iban()
    {
        using var db = InMemoryDb.Create();
        var account = SeedAccount(db);
        db.Transactions.Add(Tx(account.Id, new DateOnly(2026, 2, 10), 100m, "CRDT"));
        db.Transactions.Add(Tx(account.Id, new DateOnly(2026, 2, 20), 40m, "DBIT"));
        db.SaveChanges();

        var service = new TransactionExportService(db);
        var (content, fileName) = await service.ExportAsync(
            account.Id,
            new ExportTransactionsRequest(ExportScope.All, ExportPeriod.Custom, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)));

        Assert.NotEmpty(content);
        Assert.StartsWith(account.Iban, fileName);
        Assert.EndsWith(".xlsx", fileName);
        Assert.DoesNotContain(' ', fileName);

        Assert.All(db.Transactions.ToList(), t =>
        {
            Assert.True(t.IsExported);
            Assert.NotNull(t.ExportedAt);
        });

        using var wb = new XLWorkbook(new MemoryStream(content));
        var sheet = wb.Worksheet(1);
        // Coluna 6 = Montante: crédito positivo, débito negativo.
        Assert.Equal(100m, sheet.Cell(2, 6).GetValue<decimal>());
        Assert.Equal(-40m, sheet.Cell(3, 6).GetValue<decimal>());
    }

    [Fact]
    public async Task ExportAsync_NotExported_Scope_Ignores_Already_Exported_Rows()
    {
        using var db = InMemoryDb.Create();
        var account = SeedAccount(db);
        db.Transactions.Add(Tx(account.Id, new DateOnly(2026, 2, 10), 100m, "CRDT", exported: true));
        db.Transactions.Add(Tx(account.Id, new DateOnly(2026, 2, 12), 25m, "CRDT", exported: false));
        db.SaveChanges();

        var service = new TransactionExportService(db);
        var (content, _) = await service.ExportAsync(
            account.Id,
            new ExportTransactionsRequest(ExportScope.NotExported, ExportPeriod.Custom, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)));

        using var wb = new XLWorkbook(new MemoryStream(content));
        var sheet = wb.Worksheet(1);
        var lastRow = sheet.LastRowUsed()!.RowNumber();
        Assert.Equal(2, lastRow); // cabeçalho + 1 linha só
        Assert.Equal(25m, sheet.Cell(2, 6).GetValue<decimal>());
    }

    [Fact]
    public async Task ExportAsync_NotExported_Scope_Throws_When_Everything_Already_Exported()
    {
        using var db = InMemoryDb.Create();
        var account = SeedAccount(db);
        db.Transactions.Add(Tx(account.Id, new DateOnly(2026, 2, 10), 100m, "CRDT", exported: true));
        db.SaveChanges();

        var service = new TransactionExportService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ExportAsync(
            account.Id,
            new ExportTransactionsRequest(ExportScope.NotExported, ExportPeriod.Custom, new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28))));
    }
}
