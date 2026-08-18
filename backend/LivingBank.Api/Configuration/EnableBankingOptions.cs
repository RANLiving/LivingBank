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

    // URL público do próprio backend para onde o ASPSP redireciona após o consentimento (ex: https://livingbank-api.onrender.com/api/bank-link/callback)
    public string RedirectUrl { get; set; } = string.Empty;

    // Página do frontend para onde reencaminhar o utilizador depois de trocarmos o code pela sessão (ex: https://living-bank.vercel.app/link/callback)
    public string FrontendCallbackUrl { get; set; } = string.Empty;
}
