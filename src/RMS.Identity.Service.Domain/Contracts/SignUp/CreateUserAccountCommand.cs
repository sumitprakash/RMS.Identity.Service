namespace RMS.Identity.Service.Domain.Contracts.SignUp;

public sealed record CreateUserAccountCommand(
    Guid UserUuid,
    string Username,
    string PasswordHash,
    string? DisplayName);
