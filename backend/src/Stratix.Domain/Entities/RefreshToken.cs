namespace Stratix.Domain.Entities;

/// <summary>
/// A long-lived token that lets a client obtain a fresh short-lived access token without
/// re-entering credentials. Stored hashed; rotated on use; revoked on logout.
/// </summary>
public class RefreshToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;
}
