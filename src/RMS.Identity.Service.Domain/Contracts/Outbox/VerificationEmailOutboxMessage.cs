namespace RMS.Identity.Service.Domain.Contracts.Outbox;

public sealed record VerificationEmailOutboxMessage(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    string VerificationToken);
