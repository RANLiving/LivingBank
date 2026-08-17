using System.ComponentModel.DataAnnotations;

namespace LivingBank.Api.Dtos;

public record LoginRequest([Required] string UserName, [Required] string Password);

public record LoginResponse(string Token, DateTime ExpiresAt, string UserName, string FullName, IList<string> Roles);

public record CreateUserRequest(
    [Required] string UserName,
    [Required, EmailAddress] string Email,
    [Required] string FullName,
    [Required, MinLength(8)] string Password,
    [Required] string Role);

public record UserResponse(Guid Id, string UserName, string Email, string FullName, bool IsActive, IList<string> Roles, DateTimeOffset? LastLoginAt);
