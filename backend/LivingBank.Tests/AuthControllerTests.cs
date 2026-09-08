using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LivingBank.Api.Data;
using LivingBank.Api.Domain.Entities;
using LivingBank.Tests.TestSupport;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace LivingBank.Tests;

public class AuthControllerTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private record LoginBody(string UserName, string Password);
    private record TokenResponse(string Token, DateTime ExpiresAt, string UserName, string FullName, string[] Roles, bool PasswordExpired);

    private async Task<T> WithUserManager<T>(Func<UserManager<ApplicationUser>, Task<T>> action)
    {
        using var scope = factory.Services.CreateScope();
        var um = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return await action(um);
    }

    [Fact]
    public async Task Login_With_Seeded_Admin_Returns_Token_And_Admin_Role()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new LoginBody(ApiFactory.AdminUserName, ApiFactory.AdminPassword));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<TokenResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.Token));
        Assert.Contains(Roles.Admin, body.Roles);
        Assert.False(body.PasswordExpired); // admin isento
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Returns_401()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new LoginBody(ApiFactory.AdminUserName, "errada"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_With_Unknown_User_Returns_401()
    {
        var client = factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/auth/login",
            new LoginBody("ninguem", "seja-o-que-for"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Me_Requires_Authentication()
    {
        var client = factory.CreateClient();
        var res = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Me_Returns_Current_User_When_Authenticated()
    {
        var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginBody(ApiFactory.AdminUserName, ApiFactory.AdminPassword));
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!.Token;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var me = await res.Content.ReadFromJsonAsync<UserResponseBody>();
        Assert.Equal(ApiFactory.AdminUserName, me!.UserName);
        Assert.Contains(Roles.Admin, me.Roles);
    }

    [Fact]
    public async Task Login_Rejected_While_Password_Not_Set()
    {
        const string userName = "convidado";
        await WithUserManager(async um =>
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = "convidado@livingbank.test",
                FullName = "Convidado",
                IsActive = true,
                PasswordSet = false
            };
            await um.CreateAsync(user, "Tempor@ry123");
            return 0;
        });

        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginBody(userName, "Tempor@ry123"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var payload = await res.Content.ReadFromJsonAsync<ErrorBody>();
        Assert.Contains("não está ativa", payload!.Error);
    }

    [Fact]
    public async Task Login_Rejected_While_User_Inactive()
    {
        const string userName = "inativo";
        await WithUserManager(async um =>
        {
            var user = new ApplicationUser
            {
                UserName = userName,
                Email = "inativo@livingbank.test",
                FullName = "Inativo",
                IsActive = false,
                PasswordSet = true
            };
            await um.CreateAsync(user, "Activ@ted123");
            return 0;
        });

        var client = factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/auth/login", new LoginBody(userName, "Activ@ted123"));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    private record UserResponseBody(Guid Id, string UserName, string Email, string FullName, bool IsActive, string[] Roles, DateTimeOffset? LastLoginAt, bool PasswordExpired, bool PasswordSet);
    private record ErrorBody(string Error);
}
