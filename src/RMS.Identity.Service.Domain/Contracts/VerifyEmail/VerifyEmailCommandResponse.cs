namespace RMS.Identity.Service.Domain.Contracts.VerifyEmail;

public sealed record VerifyEmailCommandResponse(
    bool Success,
    string Message);
