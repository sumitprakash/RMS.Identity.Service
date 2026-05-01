namespace RMS.Identity.Service.Domain.Contracts.UserAccounts;

public sealed record CreateUserAccountCommand(
    Guid UserUuid,
    string Username,
    string PasswordHash,
    string? DisplayName);
