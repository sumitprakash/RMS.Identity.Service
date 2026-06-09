namespace RMS.Identity.Service.Domain.Interfaces.Notifications;

public sealed record EmailMessage(
    string To,
    string Subject,
    string Body);
