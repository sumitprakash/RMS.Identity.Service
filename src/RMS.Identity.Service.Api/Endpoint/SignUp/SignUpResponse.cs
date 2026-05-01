namespace RMS.Identity.Service.Api.Endpoint.SignUp;

public sealed record SignUpResponse(
    Guid UserUuid,
    string EmailAddress,
    string Status,
    DateTime CreatedAt);
