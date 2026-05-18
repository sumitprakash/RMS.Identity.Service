namespace RMS.Identity.Service.Domain.Contracts.SignUp;

public sealed record SignUpCommandResponse(
    Guid UserUuid,
    string Username,
    string Status,
    DateTime CreatedAt);
