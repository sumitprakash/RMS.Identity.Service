namespace RMS.Identity.Service.Domain.Entities;

/// <summary>
/// Stored refresh tokens for JWT rotation.
/// Only HASH is stored, never the raw token.
/// </summary>
public class RefreshToken
{
    public long RefreshTokenID { get; set; }

    public long UserID { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByTokenHash { get; set; }
}
