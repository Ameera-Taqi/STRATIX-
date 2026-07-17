using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

public class ProjectStage : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public StageStatus Status { get; set; } = StageStatus.PLANNED;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Progress { get; set; }
    public int OrderNumber { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
