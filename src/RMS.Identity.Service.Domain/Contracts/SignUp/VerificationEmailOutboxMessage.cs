namespace RMS.Identity.Service.Domain.Contracts.SignUp;

public sealed record VerificationEmailOutboxMessage(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    string VerificationToken);
