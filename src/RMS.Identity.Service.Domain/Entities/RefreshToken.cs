namespace RMS.Identity.Service.Domain.Entities;

public class RefreshToken
{
    public long RefreshTokenID { get; set; }

    public long UserID { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
