using System.ComponentModel.DataAnnotations;

namespace LivingBank.Api.Dtos;

public record AspspOption(string Name, string Country, string? Logo);

public record StartLinkRequest([Required] string AspspName, [Required] string AspspCountry);

public record StartLinkResponse(string Url);

public record LinkedAccountOption(string Uid, string? Iban, string? Name, string Currency);

public record SessionAccountsResponse(string SessionId, List<LinkedAccountOption> Accounts);

public record SaveLinkedAccountRequest(
    [Required] string SessionId,
    [Required] string AccountUid,
    [Required] string Iban,
    [Required] string BankName,
    [Required] string DisplayName,
    string Currency);
