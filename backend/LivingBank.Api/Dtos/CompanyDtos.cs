using System.ComponentModel.DataAnnotations;

namespace LivingBank.Api.Dtos;

public record CreateCompanyRequest([Required] string Name, [Required] string TaxId, string? Address);

public record UpdateCompanyRequest([Required] string Name, [Required] string TaxId, string? Address);

public record CompanyResponse(Guid Id, string Name, string TaxId, string? Address, bool IsActive, int BankAccountCount);
