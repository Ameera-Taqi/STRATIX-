using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

public class ProjectRisk : ITenantScoped, ISoftDeletable, IHasCreatedAt, IHasUpdatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RiskImpact Impact { get; set; }
    public RiskProbability Probability { get; set; }
    public RiskLevel RiskLevel { get; set; }
    public string? MitigationPlan { get; set; }
    public RiskStatus Status { get; set; } = RiskStatus.OPEN;
    /// <summary>Required when status becomes CLOSED.</summary>
    public string? ClosureReason { get; set; }
    /// <summary>Optional residual risk after closure (LOW / MEDIUM / HIGH).</summary>
    public RiskImpact? ResidualRisk { get; set; }
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public long OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
