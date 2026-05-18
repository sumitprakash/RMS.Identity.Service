namespace RMS.Identity.Service.Domain.Entities.UserAccounts;

public sealed record UserAccount(
    long UserId,
    Guid UserUuid,
    string Username,
    string? DisplayName,
    bool EmailVerified,
    bool IsActive,
    bool IsDeleted,
    DateTime CreatedAt);
