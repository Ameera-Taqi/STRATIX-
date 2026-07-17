using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>A request to change a project's scope/schedule, tracked through an approval flow.</summary>
public class ChangeRequest : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.PENDING;
    public TaskPriority Priority { get; set; } = TaskPriority.MEDIUM;
    public long RequestedById { get; set; }
    public User? RequestedBy { get; set; }
    public long? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
