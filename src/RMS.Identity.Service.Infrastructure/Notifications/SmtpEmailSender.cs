using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using RMS.Identity.Service.Domain.Interfaces.Notifications;

namespace RMS.Identity.Service.Infrastructure.Notifications;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly EmailDeliveryOptions _options;

    public SmtpEmailSender(IOptions<EmailDeliveryOptions> options)
    {
        _options = options.Value;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromDisplayName),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = false
        };
        mailMessage.To.Add(message.To);

        using var smtpClient = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = _options.EnableSsl
        };

        if (!string.IsNullOrWhiteSpace(_options.SmtpUsername))
        {
            smtpClient.Credentials = new NetworkCredential(
                _options.SmtpUsername,
                _options.SmtpPassword);
        }

        await smtpClient.SendMailAsync(mailMessage, cancellationToken);
    }
}
