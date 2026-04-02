namespace RMS.Identity.Service.Domain.Entities;

public class EmailVerification
{
    public long EmailVerificationID { get; set; }

    public long UserID { get; set; }

    public string TokenHash { get; set; } = null!;

    public string Purpose { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool Consumed { get; set; }
}
