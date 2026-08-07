using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>An in-app notification addressed to a single user within an organization.</summary>
public class Notification : ITenantScoped, ISoftDeletable, IHasCreatedAt
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
    /// <summary>Display name of the user who triggered the event.</summary>
    public string? ActorName { get; set; }
    public string? ProjectName { get; set; }
    /// <summary>TASK | RISK | PROJECT | STAGE | EVALUATION</summary>
    public string? EntityType { get; set; }
    public long? EntityId { get; set; }
    /// <summary>Quoted subject (task title, risk title, etc.).</summary>
    public string? EntityLabel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
