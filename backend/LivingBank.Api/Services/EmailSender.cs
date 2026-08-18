using System.Net;
using System.Net.Mail;
using LivingBank.Api.Configuration;
using Microsoft.Extensions.Options;

namespace LivingBank.Api.Services;

public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}

/// <summary>Envia email via SMTP (Gmail SMTP com App Password, ou outro relay compatível).</summary>
public class SmtpEmailSender(IOptions<EmailOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SmtpUser) || string.IsNullOrWhiteSpace(_options.SmtpPassword))
        {
            logger.LogWarning("Email:SmtpUser/SmtpPassword não configurados — email para {ToEmail} não foi enviado.", toEmail);
            throw new InvalidOperationException("Envio de email não está configurado no backend.");
        }

        using var client = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.SmtpUser, _options.SmtpPassword)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            // SmtpException.Message costuma ser genérico ("Failure sending mail.") — a causa
            // real (auth, ligação, TLS) está sempre na InnerException. Junta tudo para diagnóstico.
            var detail = ex.Message;
            var inner = ex.InnerException;
            while (inner is not null)
            {
                detail += $" → {inner.GetType().Name}: {inner.Message}";
                inner = inner.InnerException;
            }
            logger.LogError(ex, "Falha ao enviar email para {ToEmail}: {Detail}", toEmail, detail);
            throw new InvalidOperationException(detail, ex);
        }
    }
}
