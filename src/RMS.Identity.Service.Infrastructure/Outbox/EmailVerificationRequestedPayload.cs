namespace RMS.Identity.Service.Infrastructure.Outbox;

public sealed record EmailVerificationRequestedPayload(
    Guid UserUuid,
    string EmailAddress,
    string Token,
    DateTime ExpiresAt);
