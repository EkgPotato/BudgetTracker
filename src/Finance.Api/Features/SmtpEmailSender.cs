using System.Net.Mail;
using Finance.Api.Configuration;
using Finance.Api.Services;
using Microsoft.Extensions.Options;

namespace Finance.Api.Features;

public sealed class SmtpEmailSender(IOptions<MailpitOptions> options) : IEmailSender
{
    public async Task SendConfirmationAsync(string recipient, string link, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.Host) || string.IsNullOrWhiteSpace(settings.From))
            throw new InvalidOperationException("Skonfiguruj host SMTP oraz adres nadawcy.");
        if (settings.TimeoutSeconds <= 0)
            throw new InvalidOperationException("Limit czasu SMTP musi być dodatni.");

        using var client = new SmtpClient(settings.Host, settings.Port)
            {
                EnableSsl = settings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = checked(settings.TimeoutSeconds * 1_000)
            };
        using var message = new MailMessage(settings.From, recipient, "Potwierdź konto Grosik",
            $"Potwierdź adres e-mail, otwierając link: {link}");
        await client.SendMailAsync(message, cancellationToken);
    }
}
