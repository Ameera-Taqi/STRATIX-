namespace Stratix.Domain.Entities;

/// <summary>
/// A long-lived token that lets a client obtain a fresh short-lived access token without
/// re-entering credentials. Stored hashed; rotated on use; revoked on logout.
/// Tenant-scoped so tokens never leak across organizations.
/// <para>
/// Tokens belong to a <see cref="TokenFamilyId"/>. Rotation links the predecessor via
/// <see cref="ReplacedByTokenHash"/>. Presenting a replaced token is treated as possible
/// session theft: the entire family is revoked (<see cref="ReuseDetectedAt"/>).
/// </para>
/// </summary>
public class RefreshToken : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public Guid TokenFamilyId { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public DateTimeOffset? ReuseDetectedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsActive => RevokedAt == null && ExpiresAt > DateTimeOffset.UtcNow;
}
