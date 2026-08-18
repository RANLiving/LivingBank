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
public class UsersController(
    UserManager<ApplicationUser> userManager,
    IAuditService auditService,
    IUserInviteService inviteService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetAll()
    {
        var users = userManager.Users.OrderBy(u => u.FullName).ToList();
        var result = new List<UserResponse>();
        foreach (var u in users)
        {
            var roles = await userManager.GetRolesAsync(u);
            result.Add(new UserResponse(u.Id, u.UserName!, u.Email!, u.FullName, u.IsActive, roles, u.LastLoginAt, PasswordPolicy.IsExpired(u, roles), u.PasswordSet));
        }
        return Ok(result);
    }

    /// <summary>
    /// Cria o utilizador sem definir password — envia logo um email de convite com um link
    /// para o próprio utilizador definir a password. Só consegue entrar depois disso.
    /// </summary>
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
            PasswordSet = false,
            PasswordChangedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await userManager.AddToRoleAsync(user, request.Role);

        try
        {
            await inviteService.SendInviteAsync(user);
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                user = new UserResponse(user.Id, user.UserName!, user.Email!, user.FullName, user.IsActive, new[] { request.Role }, null, false, false),
                warning = $"Utilizador criado, mas o email de convite falhou: {ex.Message}"
            });
        }

        await auditService.LogAsync(GetCurrentUserId(), "User.Create", $"novo utilizador={request.UserName}, role={request.Role}");

        return Ok(new UserResponse(user.Id, user.UserName!, user.Email!, user.FullName, user.IsActive, [request.Role], null, false, false));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<UserResponse>> Update(Guid id, UpdateUserRequest request)
    {
        if (!Roles.All.Contains(request.Role))
            return BadRequest(new { error = $"Role inválida. Válidas: {string.Join(", ", Roles.All)}" });

        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var currentRoles = await userManager.GetRolesAsync(user);
        if (currentRoles.Contains(Roles.Admin) && request.Role != Roles.Admin)
            return BadRequest(new { error = "O utilizador Admin não pode ter a role alterada." });

        user.Email = request.Email;
        user.FullName = request.FullName;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
            return BadRequest(new { errors = updateResult.Errors.Select(e => e.Description) });

        if (!currentRoles.Contains(request.Role))
        {
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, request.Role);
        }

        await auditService.LogAsync(GetCurrentUserId(), "User.Update", $"utilizador={user.UserName} role={request.Role}");

        var newRoles = await userManager.GetRolesAsync(user);
        return Ok(new UserResponse(user.Id, user.UserName!, user.Email!, user.FullName, user.IsActive, newRoles, user.LastLoginAt, PasswordPolicy.IsExpired(user, newRoles), user.PasswordSet));
    }

    /// <summary>Invalida a password atual e reenvia o email de convite/definição de password.</summary>
    [HttpPost("{id}/resend-invite")]
    public async Task<IActionResult> ResendInvite(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        await inviteService.SendInviteAsync(user);
        await auditService.LogAsync(GetCurrentUserId(), "User.ResendInvite", $"utilizador={user.UserName}");
        return NoContent();
    }

    /// <summary>Elimina definitivamente um utilizador (diferente dos movimentos bancários, nunca eliminados).</summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await userManager.FindByIdAsync(id.ToString());
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(Roles.Admin))
            return BadRequest(new { error = "O utilizador Admin não pode ser eliminado." });

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        await auditService.LogAsync(GetCurrentUserId(), "User.Delete", $"utilizador={user.UserName}");
        return NoContent();
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
