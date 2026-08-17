namespace LivingBank.Api.Configuration;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "LivingBank";
    public string Audience { get; set; } = "LivingBank.Clients";
    public int ExpiryMinutes { get; set; } = 60;
}
