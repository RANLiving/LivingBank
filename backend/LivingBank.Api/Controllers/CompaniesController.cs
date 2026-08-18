using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Dtos;
using LivingBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LivingBank.Api.Controllers;

[ApiController]
[Route("api/companies")]
[Authorize]
public class CompaniesController(AppDbContext db, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CompanyResponse>>> GetAll()
    {
        var companies = await db.Companies
            .Select(c => new CompanyResponse(c.Id, c.Name, c.TaxId, c.Address, c.IsActive, c.BankAccounts.Count()))
            .OrderBy(c => c.Name)
            .ToListAsync();
        return Ok(companies);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<ActionResult<CompanyResponse>> Create(CreateCompanyRequest request)
    {
        if (await db.Companies.AnyAsync(c => c.TaxId == request.TaxId))
            return Conflict(new { error = "Já existe uma empresa com esse NIF." });

        var company = new Company { Name = request.Name, TaxId = request.TaxId, Address = request.Address };
        db.Companies.Add(company);
        await db.SaveChangesAsync();

        await auditService.LogAsync(GetCurrentUserId(), "Company.Create", $"empresa={company.Name} nif={company.TaxId}");
        return Ok(new CompanyResponse(company.Id, company.Name, company.TaxId, company.Address, company.IsActive, 0));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<ActionResult<CompanyResponse>> Update(Guid id, UpdateCompanyRequest request)
    {
        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound();

        if (await db.Companies.AnyAsync(c => c.TaxId == request.TaxId && c.Id != id))
            return Conflict(new { error = "Já existe outra empresa com esse NIF." });

        company.Name = request.Name;
        company.TaxId = request.TaxId;
        company.Address = request.Address;
        await db.SaveChangesAsync();

        await auditService.LogAsync(GetCurrentUserId(), "Company.Update", $"empresa={company.Name} nif={company.TaxId}");

        var count = await db.BankAccounts.CountAsync(a => a.CompanyId == id);
        return Ok(new CompanyResponse(company.Id, company.Name, company.TaxId, company.Address, company.IsActive, count));
    }

    [HttpPatch("{id}/deactivate")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound();
        company.IsActive = false;
        await db.SaveChangesAsync();
        await auditService.LogAsync(GetCurrentUserId(), "Company.Deactivate", $"empresa={company.Name}");
        return NoContent();
    }

    [HttpPatch("{id}/activate")]
    [Authorize(Policy = Permissions.ManageBankAccounts)]
    public async Task<IActionResult> Activate(Guid id)
    {
        var company = await db.Companies.FindAsync(id);
        if (company is null) return NotFound();
        company.IsActive = true;
        await db.SaveChangesAsync();
        await auditService.LogAsync(GetCurrentUserId(), "Company.Activate", $"empresa={company.Name}");
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? Guid.Parse(claim) : null;
    }
}
