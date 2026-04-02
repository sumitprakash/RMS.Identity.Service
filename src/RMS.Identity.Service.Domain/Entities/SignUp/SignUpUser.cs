namespace RMS.Identity.Service.Domain.Entities.SignUp;

public sealed record SignUpUser(
    Guid UserUuid,
    string Username,
    string? DisplayName,
    string Status,
    DateTime CreatedAt);
