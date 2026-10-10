using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Taleshaven.Infrastructure.Identity;

namespace Taleshaven.Web.Components.Account;

/// <summary>
/// Inställningarna under <c>Email</c> i konfigurationen (B70). Utan <see cref="Host"/> skickas inga mejl
/// (<see cref="IdentityNoOpEmailSender"/>). I utvecklingsmiljön pekar de på Mailpit i docker-compose.
/// </summary>
public sealed class EmailOptions
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; } = true;
    public string From { get; set; } = "no-reply@taleshaven.local";
    public string FromName { get; set; } = "Taleshaven";
}

/// <summary>
/// Skickar kontots mejl (bekräftelse, nytt lösenord, byte av e-post) med SMTP (B70). Ett fel vid sändningen loggas men
/// avbryter inte det användaren håller på med; sidorna säger bara "kolla din e-post".
/// </summary>
internal sealed class SmtpEmailSender(EmailOptions options, ILogger<SmtpEmailSender> logger) : IEmailSender<ApplicationUser>, IMessageEmailSender
{
    public Task SendNewMessageAsync(string email, string recipientName, string senderName, string link) =>
        SendAsync(email, $"New message from {senderName}",
            $"<p>Hi {WebUtility.HtmlEncode(recipientName)},</p><p>{WebUtility.HtmlEncode(senderName)} has sent you a message on Taleshaven. " +
            $"<a href='{link}'>Read it here</a>.</p><p>You can turn these emails off under your profile.</p>");

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        SendAsync(email, "Confirm your email address",
            $"<p>Hi {WebUtility.HtmlEncode(user.DisplayName)},</p><p>Confirm your email address for Taleshaven by " +
            $"<a href='{confirmationLink}'>clicking here</a>.</p><p>If you didn't ask for this, you can ignore this email.</p>");

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        SendAsync(email, "Reset your password",
            $"<p>Hi {WebUtility.HtmlEncode(user.DisplayName)},</p><p>Reset your Taleshaven password by " +
            $"<a href='{resetLink}'>clicking here</a>.</p><p>If you didn't ask for this, you can ignore this email.</p>");

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        SendAsync(email, "Reset your password",
            $"<p>Hi {WebUtility.HtmlEncode(user.DisplayName)},</p><p>Your code for resetting your Taleshaven password: " +
            $"<strong>{WebUtility.HtmlEncode(resetCode)}</strong></p>");

    private async Task SendAsync(string to, string subject, string html)
    {
        using var message = new MailMessage
        {
            From = new MailAddress(options.From, options.FromName),
            Subject = subject,
            Body = html,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        using var client = new SmtpClient(options.Host, options.Port) { EnableSsl = options.EnableSsl };
        if (!string.IsNullOrEmpty(options.UserName))
            client.Credentials = new NetworkCredential(options.UserName, options.Password);

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or IOException)
        {
            logger.LogError(ex, "Mejlet \"{Subject}\" kunde inte skickas.", subject);
        }
    }
}
