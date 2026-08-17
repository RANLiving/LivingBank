namespace LivingBank.Api.Configuration;

public class EnableBankingOptions
{
    public const string SectionName = "EnableBanking";

    public string ApplicationId { get; set; } = string.Empty;
    public string PrivateKeyPem { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.enablebanking.com";

    // Segredo partilhado com o workflow do GitHub Actions para autorizar o trigger externo do cron
    public string ExternalCronSecret { get; set; } = string.Empty;

    // Limite diário de leituras por conta (requisito de negócio)
    public int MaxDailySyncsPerAccount { get; set; } = 4;
}
