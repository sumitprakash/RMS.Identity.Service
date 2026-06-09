namespace RMS.Identity.Service.Api.Endpoint.Users.VerifyEmail;

public sealed record VerifyEmailResponse(
    bool Success,
    string Message);
