namespace RMS.Identity.Service.Domain.Entities.Auth;

public sealed record AuthenticatedUser(
    long UserId,
    Guid UserUuid,
    Guid? CompanyUuid,
    string Username,
    string PasswordHash,
    string? DisplayName,
    bool EmailVerified,
    bool IsActive,
    bool IsDeleted,
    DateTime? LockedUntil,
    DateTime CreatedAt,
    IReadOnlyCollection<string> Roles)
{
    public bool PasswordSetupRequired { get; init; }

    public string Status
    {
        get
        {
            if (!IsActive || IsDeleted)
            {
                return "suspended";
            }

            return EmailVerified ? "active" : "pending";
        }
    }
}
