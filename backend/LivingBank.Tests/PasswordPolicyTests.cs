using LivingBank.Api.Domain.Entities;
using LivingBank.Api.Services;

namespace LivingBank.Tests;

public class PasswordPolicyTests
{
    private static ApplicationUser UserChangedDaysAgo(int days) => new()
    {
        PasswordChangedAt = DateTimeOffset.UtcNow.AddDays(-days)
    };

    [Fact]
    public void IsExpired_False_When_Password_Is_Recent()
    {
        var expired = PasswordPolicy.IsExpired(UserChangedDaysAgo(10), new List<string> { Roles.Manager });
        Assert.False(expired);
    }

    [Fact]
    public void IsExpired_True_When_Password_Older_Than_60_Days()
    {
        var expired = PasswordPolicy.IsExpired(UserChangedDaysAgo(61), new List<string> { Roles.Viewer });
        Assert.True(expired);
    }

    [Fact]
    public void IsExpired_False_Just_Under_60_Days()
    {
        var user = new ApplicationUser { PasswordChangedAt = DateTimeOffset.UtcNow.AddDays(-60).AddMinutes(30) };
        var expired = PasswordPolicy.IsExpired(user, new List<string> { Roles.Viewer });
        Assert.False(expired);
    }

    [Fact]
    public void IsExpired_True_Just_Over_60_Days()
    {
        var user = new ApplicationUser { PasswordChangedAt = DateTimeOffset.UtcNow.AddDays(-60).AddMinutes(-30) };
        var expired = PasswordPolicy.IsExpired(user, new List<string> { Roles.Viewer });
        Assert.True(expired);
    }

    [Fact]
    public void IsExpired_False_For_Admin_Even_When_Very_Old()
    {
        var expired = PasswordPolicy.IsExpired(UserChangedDaysAgo(365), new List<string> { Roles.Admin });
        Assert.False(expired);
    }

    [Fact]
    public void IsExpired_True_For_User_With_Multiple_NonAdmin_Roles_When_Old()
    {
        var expired = PasswordPolicy.IsExpired(
            UserChangedDaysAgo(90),
            new List<string> { Roles.Manager, Roles.Viewer });
        Assert.True(expired);
    }

    [Fact]
    public void MaxAge_Is_60_Days()
    {
        Assert.Equal(TimeSpan.FromDays(60), PasswordPolicy.MaxAge);
    }
}
