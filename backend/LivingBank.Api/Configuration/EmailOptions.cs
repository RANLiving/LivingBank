namespace LivingBank.Api.Configuration;

public class EmailOptions
{
    public const string SectionName = "Email";

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;

    public string FromAddress { get; set; } = string.Empty;
    public string FromName { get; set; } = "LivingBank";

    // URL do frontend usada para construir o link "/set-password?..." enviado por email.
    public string FrontendBaseUrl { get; set; } = string.Empty;
}
