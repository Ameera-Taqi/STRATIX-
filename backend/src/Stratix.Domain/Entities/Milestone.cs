using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>
/// A key checkpoint within a project. Tenant-scoped like every other project record.
/// </summary>
public class Milestone : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public MilestoneStatus Status { get; set; } = MilestoneStatus.PENDING;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
