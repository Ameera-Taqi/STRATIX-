namespace Stratix.Domain.Entities;

public class PasswordResetToken
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public User User { get; set; } = null!;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? UsedAt { get; set; }
    public string? RequestIp { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
