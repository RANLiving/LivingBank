using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LivingBank.Api.Configuration;
using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LivingBank.Tests;

public class JwtTokenServiceTests
{
    private static readonly JwtOptions Options = new()
    {
        Secret = "test-secret-000000000000000000000000000000000000000000000000",
        Issuer = "LivingBank",
        Audience = "LivingBank.Clients",
        ExpiryMinutes = 60
    };

    private static JwtTokenService Service() => new(Microsoft.Extensions.Options.Options.Create(Options));

    private static ApplicationUser SampleUser() => new()
    {
        Id = Guid.NewGuid(),
        UserName = "joana",
        Email = "joana@livingtours.com",
        FullName = "Joana Silva"
    };

    [Fact]
    public void Token_Carries_Identity_And_Role_Claims()
    {
        var user = SampleUser();
        var token = Service().GenerateToken(user, new List<string> { Roles.Manager, Roles.Viewer }, passwordExpired: false);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == ClaimTypes.NameIdentifier).Value);
        Assert.Equal("joana", jwt.Claims.Single(c => c.Type == ClaimTypes.Name).Value);
        Assert.Equal("joana@livingtours.com", jwt.Claims.Single(c => c.Type == ClaimTypes.Email).Value);
        Assert.Equal("Joana Silva", jwt.Claims.Single(c => c.Type == "full_name").Value);
        Assert.Equal("false", jwt.Claims.Single(c => c.Type == "pwd_expired").Value);

        var roles = jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToArray();
        Assert.Contains(Roles.Manager, roles);
        Assert.Contains(Roles.Viewer, roles);
    }

    [Fact]
    public void Token_Sets_Pwd_Expired_Flag()
    {
        var token = Service().GenerateToken(SampleUser(), new List<string> { Roles.Viewer }, passwordExpired: true);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("true", jwt.Claims.Single(c => c.Type == "pwd_expired").Value);
    }

    [Fact]
    public void Token_Is_Signed_With_Configured_Secret_And_Validates()
    {
        var token = Service().GenerateToken(SampleUser(), new List<string> { Roles.Admin }, passwordExpired: false);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Options.Issuer,
            ValidAudience = Options.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Options.Secret))
        };

        var principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out var validated);

        Assert.True(principal.IsInRole(Roles.Admin));
        Assert.Equal(SecurityAlgorithms.HmacSha256, ((JwtSecurityToken)validated).Header.Alg);
    }

    [Fact]
    public void Token_Fails_Validation_Under_Wrong_Secret()
    {
        var token = Service().GenerateToken(SampleUser(), new List<string> { Roles.Admin }, passwordExpired: false);

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("a-completely-different-secret-key-value-1234567890"))
        };

        Assert.ThrowsAny<SecurityTokenException>(
            () => new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _));
    }

    [Fact]
    public void Token_Expiry_Matches_Configured_Window()
    {
        var before = DateTime.UtcNow;
        var token = Service().GenerateToken(SampleUser(), new List<string>(), passwordExpired: false);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        var expectedMin = before.AddMinutes(Options.ExpiryMinutes).AddSeconds(-5);
        var expectedMax = DateTime.UtcNow.AddMinutes(Options.ExpiryMinutes).AddSeconds(5);
        Assert.InRange(jwt.ValidTo, expectedMin, expectedMax);
    }
}
