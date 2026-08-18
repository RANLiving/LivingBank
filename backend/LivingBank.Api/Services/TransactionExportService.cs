using ClosedXML.Excel;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Dtos;
using Microsoft.EntityFrameworkCore;

namespace LivingBank.Api.Services;

public interface ITransactionExportService
{
    Task<(byte[] Content, string FileName)> ExportAsync(Guid bankAccountId, ExportTransactionsRequest request, CancellationToken ct = default);
}

public class TransactionExportService(AppDbContext db) : ITransactionExportService
{
    public async Task<(byte[] Content, string FileName)> ExportAsync(Guid bankAccountId, ExportTransactionsRequest request, CancellationToken ct = default)
    {
        var account = await db.BankAccounts.FirstOrDefaultAsync(a => a.Id == bankAccountId, ct)
            ?? throw new KeyNotFoundException($"Conta bancária {bankAccountId} não encontrada.");

        var (from, to) = ResolvePeriod(request.Period, request.From, request.To);

        var query = db.Transactions.Where(t => t.BankAccountId == bankAccountId && t.BookingDate >= from && t.BookingDate <= to);
        if (request.Scope == ExportScope.NotExported)
            query = query.Where(t => !t.IsExported);

        var transactions = await query.OrderBy(t => t.BookingDate).ToListAsync(ct);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Movimentos");

        string[] headers = ["Data valor", "Data movimento", "Descrição", "Contraparte", "IBAN contraparte", "Montante", "Moeda", "Tipo", "Estado", "Já exportado antes"];
        for (var col = 0; col < headers.Length; col++)
        {
            sheet.Cell(1, col + 1).Value = headers[col];
            sheet.Cell(1, col + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var t in transactions)
        {
            if (t.ValueDate.HasValue) sheet.Cell(row, 1).Value = t.ValueDate.Value.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 2).Value = t.BookingDate.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 3).Value = t.Description;
            sheet.Cell(row, 4).Value = t.CounterpartyName ?? "";
            sheet.Cell(row, 5).Value = t.CounterpartyIban ?? "";
            sheet.Cell(row, 6).Value = t.CreditDebitIndicator == "CRDT" ? t.Amount : -t.Amount;
            sheet.Cell(row, 7).Value = t.Currency;
            sheet.Cell(row, 8).Value = t.CreditDebitIndicator == "CRDT" ? "Crédito" : "Débito";
            sheet.Cell(row, 9).Value = t.Status;
            sheet.Cell(row, 10).Value = t.IsExported ? "Sim" : "Não";
            row++;
        }

        sheet.Column(1).Style.DateFormat.Format = "dd/MM/yyyy";
        sheet.Column(2).Style.DateFormat.Format = "dd/MM/yyyy";
        sheet.Column(6).Style.NumberFormat.Format = "#,##0.00";
        sheet.Columns().AdjustToContents();

        var now = DateTimeOffset.UtcNow;
        foreach (var t in transactions)
        {
            t.IsExported = true;
            t.ExportedAt = now;
        }
        await db.SaveChangesAsync(ct);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        var fileName = $"{account.Iban}-{account.DisplayName}-{from:yyyyMMdd}-{to:yyyyMMdd}.xlsx".Replace(' ', '-');
        return (stream.ToArray(), fileName);
    }

    private static (DateOnly From, DateOnly To) ResolvePeriod(ExportPeriod period, DateOnly? from, DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        switch (period)
        {
            case ExportPeriod.Custom:
                if (!from.HasValue || !to.HasValue)
                    throw new InvalidOperationException("Período personalizado exige data de início e fim.");
                return (from.Value, to.Value);

            case ExportPeriod.PreviousMonth:
            {
                var firstOfThisMonth = new DateOnly(today.Year, today.Month, 1);
                var firstOfPrevMonth = firstOfThisMonth.AddMonths(-1);
                var lastOfPrevMonth = firstOfThisMonth.AddDays(-1);
                return (firstOfPrevMonth, lastOfPrevMonth);
            }

            case ExportPeriod.PreviousQuarter:
            {
                var currentQuarter = (today.Month - 1) / 3; // 0..3
                var firstOfThisQuarter = new DateOnly(today.Year, currentQuarter * 3 + 1, 1);
                var firstOfPrevQuarter = firstOfThisQuarter.AddMonths(-3);
                var lastOfPrevQuarter = firstOfThisQuarter.AddDays(-1);
                return (firstOfPrevQuarter, lastOfPrevQuarter);
            }

            case ExportPeriod.PreviousSemester:
            {
                var firstOfThisSemester = today.Month <= 6 ? new DateOnly(today.Year, 1, 1) : new DateOnly(today.Year, 7, 1);
                var firstOfPrevSemester = firstOfThisSemester.AddMonths(-6);
                var lastOfPrevSemester = firstOfThisSemester.AddDays(-1);
                return (firstOfPrevSemester, lastOfPrevSemester);
            }

            case ExportPeriod.CurrentYear:
                return (new DateOnly(today.Year, 1, 1), today);

            default:
                throw new InvalidOperationException($"Período de exportação desconhecido: {period}");
        }
    }
}
