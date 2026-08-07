using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

public class EvaluationPeriod : ITenantScoped, ISoftDeletable, IHasCreatedAt, IHasUpdatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public EvaluationPeriodStatus Status { get; set; } = EvaluationPeriodStatus.DRAFT;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public ICollection<EmployeeEvaluation> Evaluations { get; set; } = new List<EmployeeEvaluation>();
    public ICollection<PeriodKpiSnapshot> KpiSnapshots { get; set; } = new List<PeriodKpiSnapshot>();
}

public class KpiDefinition : ITenantScoped, ISoftDeletable, IHasCreatedAt, IHasUpdatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Weight { get; set; } = 1m;
    /// <summary>Optional target/threshold used when the period snapshot is taken.</summary>
    public decimal? TargetValue { get; set; }
    /// <summary>Formula key or expression (defaults to <see cref="Code"/> when empty).</summary>
    public string? Formula { get; set; }
    public bool HigherIsBetter { get; set; } = true;
    public bool IsActive { get; set; } = true;
    /// <summary>
    /// Optional role scope. Null = org-wide (all roles). Weights are validated to sum 100% per scope.
    /// </summary>
    public UserRole? AppliesToRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

/// <summary>
/// Frozen KPI definition for an evaluation period. Captured when the period is opened so later
/// admin edits to live definitions do not change historical interpretation.
/// </summary>
public class PeriodKpiSnapshot : ITenantScoped, IHasCreatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long PeriodId { get; set; }
    public EvaluationPeriod? Period { get; set; }
    public long KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Weight { get; set; } = 1m;
    public decimal? TargetValue { get; set; }
    public string Formula { get; set; } = string.Empty;
    public bool HigherIsBetter { get; set; } = true;
    public UserRole? AppliesToRole { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public class EmployeeEvaluation : ITenantScoped, ISoftDeletable, IHasCreatedAt, IHasUpdatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long PeriodId { get; set; }
    public EvaluationPeriod? Period { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }
    public EmployeeEvaluationStatus Status { get; set; } = EmployeeEvaluationStatus.DRAFT;
    public decimal? OverallScore { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public long? ReviewedById { get; set; }
    public User? ReviewedBy { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public long? ApprovedById { get; set; }
    public User? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    /// <summary>Set when an APPROVED evaluation is formally reopened; cleared on next approval.</summary>
    public string? ReopenReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public ICollection<EmployeeKpiResult> Results { get; set; } = new List<EmployeeKpiResult>();
}

public class EmployeeKpiResult : ITenantScoped, ISoftDeletable, IHasCreatedAt, IHasUpdatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long EvaluationId { get; set; }
    public EmployeeEvaluation? Evaluation { get; set; }
    public long KpiDefinitionId { get; set; }
    public KpiDefinition? KpiDefinition { get; set; }

    /// <summary>Frozen definition name at calculation time (period snapshot).</summary>
    public string SnapshotName { get; set; } = string.Empty;
    /// <summary>Frozen KPI code / formula key.</summary>
    public string SnapshotCode { get; set; } = string.Empty;
    /// <summary>Frozen weight used for OverallScore.</summary>
    public decimal SnapshotWeight { get; set; } = 1m;
    /// <summary>Frozen target at calculation time.</summary>
    public decimal? SnapshotTarget { get; set; }
    /// <summary>Frozen formula expression / key.</summary>
    public string SnapshotFormula { get; set; } = string.Empty;
    public bool SnapshotHigherIsBetter { get; set; } = true;

    public decimal CalculatedValue { get; set; }
    public decimal? AdjustedValue { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public class TaskQualityEvaluation : ITenantScoped, ISoftDeletable, IHasCreatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long TaskId { get; set; }
    public TaskItem? Task { get; set; }
    public long EvaluatorId { get; set; }
    public User? Evaluator { get; set; }
    public int QualityScore { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
