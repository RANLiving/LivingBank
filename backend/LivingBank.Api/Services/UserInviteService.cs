using System.Security.Cryptography;
using LivingBank.Api.Configuration;
using LivingBank.Api.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LivingBank.Api.Services;

public interface IUserInviteService
{
    /// <summary>
    /// Invalida a password atual do utilizador (substitui por uma aleatória descartada,
    /// nunca comunicada) e envia um email com um link para definir uma nova password.
    /// Usado tanto na criação de utilizadores como no reenvio forçado de convite.
    /// </summary>
    Task SendInviteAsync(ApplicationUser user, CancellationToken ct = default);
}

public class UserInviteService(
    UserManager<ApplicationUser> userManager,
    IEmailSender emailSender,
    IOptions<EmailOptions> options) : IUserInviteService
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendInviteAsync(ApplicationUser user, CancellationToken ct = default)
    {
        // Gera uma password aleatória forte que nunca é comunicada a ninguém — invalida
        // qualquer password anterior. O utilizador só consegue entrar depois de definir
        // a sua própria password através do link enviado por email.
        var discardedPassword = GenerateRandomPassword();

        if (await userManager.HasPasswordAsync(user))
            await userManager.RemovePasswordAsync(user);
        await userManager.AddPasswordAsync(user, discardedPassword);

        user.PasswordSet = false;
        await userManager.UpdateAsync(user);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var link = $"{_options.FrontendBaseUrl.TrimEnd('/')}/set-password?userId={user.Id}&token={Uri.EscapeDataString(token)}";

        var html = $"""
            <p>Olá {user.FullName},</p>
            <p>Foi criada (ou reiniciada) uma conta para ti na <strong>LivingBank</strong>.</p>
            <p>Para poderes entrar, define a tua password através do link abaixo:</p>
            <p><a href="{link}">{link}</a></p>
            <p>Este link só pode ser usado uma vez. Se não esperavas este email, ignora-o.</p>
            <p>— LivingBank</p>
            """;

        var plainText = $"""
            Olá {user.FullName},

            Foi criada (ou reiniciada) uma conta para ti na LivingBank.

            Para poderes entrar, define a tua password através do link abaixo:
            {link}

            Este link só pode ser usado uma vez. Se não esperavas este email, ignora-o.

            — LivingBank
            """;

        await emailSender.SendAsync(user.Email!, "LivingBank — definir a tua password", html, plainText, ct);
    }

    private static string GenerateRandomPassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes) + "aA1!";
    }
}
