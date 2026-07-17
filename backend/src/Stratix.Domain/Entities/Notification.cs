using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>An in-app notification addressed to a single user within an organization.</summary>
public class Notification : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public NotificationType Type { get; set; } = NotificationType.INFO;
    public bool IsRead { get; set; }
    public string? Link { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
