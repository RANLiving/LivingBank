using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Dtos;
using LivingBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LivingBank.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = Permissions.ManageUsers)]
public class UsersController(UserManager<ApplicationUser> userManager, IAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll()
    {
        var users = userManager.Users.ToList();
        var result = new List<UserResponse>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            result.Add(new UserResponse(u.Id, u.UserName!, u.Email!, u.FullName, u.IsActive, roles, u.LastLoginAt, PasswordPolicy.IsExpired(u, roles)));
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> Create(CreateUserRequest request)
    {
        if (!Roles.All.Contains(request.Role))
            return BadRequest(new { error = $"Role inválida. Válidas: {string.Join(", ", Roles.All)}" });

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = request.Email,
            FullName = request.FullName,
            IsActive = true,
            PasswordChangedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await userManager.AddToRoleAsync(user, request.Role);
        await auditService.LogAsync(GetCurrentUserId(), "User.Create", $"novo utilizador={request.UserName}, role={request.Role}");

        return Ok(new UserResponse(user.Id, user.UserName!, user.Email!, user.FullName, user.IsActive, [request.Role], null, false));
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.Admin))
            return BadRequest(new { error = "O utilizador Admin não pode ser desativado." });

        user.IsActive = false;
        await userManager.UpdateAsync(user);
        await auditService.LogAsync(GetCurrentUserId(), "User.Deactivate", $"utilizador={user.UserName}");
        return NoContent();
    }

    [HttpPatch("{id}/activate")]
    public async Task<IActionResult> Activate(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        user.IsActive = true;
        await userManager.UpdateAsync(user);
        await auditService.LogAsync(GetCurrentUserId(), "User.Activate", $"utilizador={user.UserName}");
        return NoContent();
    }

    private Guid? GetCurrentUserId()
    {
        var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? Guid.Parse(claim) : null;
    }
}
