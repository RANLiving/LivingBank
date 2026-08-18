using System.ComponentModel.DataAnnotations;

namespace LivingBank.Api.Dtos;

public record LoginRequest([Required] string UserName, [Required] string Password);

public record LoginResponse(string Token, DateTime ExpiresAt, string UserName, string FullName, IList<string> Roles, bool PasswordExpired);

public record CreateUserRequest(
    [Required] string UserName,
    [Required, EmailAddress] string Email,
    [Required] string FullName,
    [Required, MinLength(8)] string Password,
    [Required] string Role);

public record UserResponse(Guid Id, string UserName, string Email, string FullName, bool IsActive, IList<string> Roles, DateTimeOffset? LastLoginAt, bool PasswordExpired);

public record ChangePasswordRequest([Required] string CurrentPassword, [Required, MinLength(8)] string NewPassword);
