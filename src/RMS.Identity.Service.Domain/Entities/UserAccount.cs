namespace RMS.Identity.Service.Domain.Entities;

public class UserAccount
{
    public long UserID { get; set; }

    public Guid UserUUID { get; set; }

    public long? CompanyID { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? DisplayName { get; set; }

    public bool EmailVerified { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public int FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }
}
