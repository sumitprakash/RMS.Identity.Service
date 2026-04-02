namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed record SignUpResponse(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    string Status,
    DateTime CreatedAt);
