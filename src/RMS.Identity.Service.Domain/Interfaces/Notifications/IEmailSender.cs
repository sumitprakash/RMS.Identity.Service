namespace RMS.Identity.Service.Domain.Interfaces.Notifications;

public interface IEmailSender
{
    Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken);
}
