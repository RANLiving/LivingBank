using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Dtos;
using LivingBank.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LivingBank.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    IAuditService auditService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        var user = await userManager.FindByNameAsync(request.UserName);
        if (user is null || !user.IsActive)
            return Unauthorized(new { error = "Credenciais inválidas." });

        var result = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            await auditService.LogAsync(null, "Auth.LoginFailed", $"userName={request.UserName}");
            return Unauthorized(new { error = "Credenciais inválidas." });
        }

        var roles = await userManager.GetRolesAsync(user);
        var passwordExpired = PasswordPolicy.IsExpired(user, roles);
        var token = jwtTokenService.GenerateToken(user, roles, passwordExpired);

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        await auditService.LogAsync(user.Id, "Auth.Login");

        return Ok(new LoginResponse(token, DateTime.UtcNow.AddMinutes(60), user.UserName!, user.FullName, roles, passwordExpired));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserResponse>> Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var roles = await userManager.GetRolesAsync(user);
        var passwordExpired = PasswordPolicy.IsExpired(user, roles);
        return Ok(new UserResponse(user.Id, user.UserName!, user.Email!, user.FullName, user.IsActive, roles, user.LastLoginAt, passwordExpired));
    }

    /// <summary>Qualquer utilizador autenticado pode mudar a própria password a qualquer momento.</summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<LoginResponse>> ChangePassword(ChangePasswordRequest request)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value;
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return NotFound();

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(new { errors = result.Errors.Select(e => e.Description) });

        user.PasswordChangedAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        await auditService.LogAsync(user.Id, "Auth.ChangePassword");

        var roles = await userManager.GetRolesAsync(user);
        var token = jwtTokenService.GenerateToken(user, roles, passwordExpired: false);
        return Ok(new LoginResponse(token, DateTime.UtcNow.AddMinutes(60), user.UserName!, user.FullName, roles, false));
    }
}
