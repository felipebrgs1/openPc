using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace OpenPc.Scraper.Email;

/// <summary>Envio de e-mail transacional (alertas de preço, M6).</summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct);
}

/// <summary>
/// Envio via SMTP configurado (Smtp:Host/Port/Username/Password/From).
/// Sem host configurado, apenas loga (modo dev/staging) e retorna sucesso.
/// </summary>
public sealed class SmtpEmailSender(
    IConfiguration config,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct)
    {
        var host = config["Smtp:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogInformation("[email:dry-run] para={To} assunto={Subject} corpo={Body}",
                to, subject, htmlBody);
            return;
        }

        var port = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        var username = config["Smtp:Username"];
        var password = config["Smtp:Password"];
        var from = config["Smtp:From"] ?? "OpenPC <no-reply@openpc.example>";

        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(host, port,
            username is null ? SecureSocketOptions.StartTlsWhenAvailable : SecureSocketOptions.StartTls, ct);
        if (username is not null)
            await client.AuthenticateAsync(username, password ?? "", ct);
        await client.SendAsync(message, ct);
        await client.DisconnectAsync(true, ct);
    }
}
