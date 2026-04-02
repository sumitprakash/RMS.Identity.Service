namespace RMS.Identity.Service.Api.Contracts;

public sealed record SignupRequest(
    string Username,
    string Password,
    string? DisplayName,
    string? Phone);
