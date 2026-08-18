using System.ComponentModel.DataAnnotations;

namespace LivingBank.Api.Dtos;

public record CreateBankAccountRequest(
    [Required] string EnableBankingAccountId,
    [Required] string Iban,
    [Required] string BankName,
    [Required] string DisplayName,
    string Currency,
    [Required] string AspspName,
    string AspspCountry,
    string? SessionId,
    DateTimeOffset? ConsentValidUntil);

public record BankAccountResponse(
    Guid Id, string Iban, string BankName, string DisplayName, string Currency,
    bool IsActive, DateTimeOffset? ConsentValidUntil, decimal? LatestBalance, DateTimeOffset? LatestBalanceDate,
    int TransactionCount);

public record BalanceResponse(long Id, string BalanceType, decimal Amount, string Currency, DateTimeOffset ReferenceDate, DateTimeOffset FetchedAt);

public record TransactionResponse(
    long Id, decimal Amount, string Currency, string CreditDebitIndicator,
    DateOnly BookingDate, DateOnly? ValueDate, string Description, string? CounterpartyName, string Status,
    bool IsExported);

public record SyncScheduleRequest(TimeOnly Time1, TimeOnly Time2, TimeOnly Time3, TimeOnly Time4);

public enum ExportScope { All, NotExported }
public enum ExportPeriod { Custom, PreviousMonth, PreviousQuarter, PreviousSemester, CurrentYear }

public record ExportTransactionsRequest(ExportScope Scope, ExportPeriod Period, DateOnly? From, DateOnly? To);

public record SyncLogResponse(
    long Id, Guid BankAccountId, string SyncTrigger, string Status, string? ErrorMessage,
    int BalancesFetched, int TransactionsFetched, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt);
