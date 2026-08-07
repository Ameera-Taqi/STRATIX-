namespace Stratix.Application.DTOs.Kpi;

public record EvaluationPeriodResponse(
    long Id, string Name, DateOnly StartDate, DateOnly EndDate, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt,
    IReadOnlyList<PeriodKpiSnapshotResponse>? KpiSnapshots = null);

public record PeriodKpiSnapshotResponse(
    long Id, long KpiDefinitionId, string Code, string Name, decimal Weight,
    decimal? TargetValue, string Formula, bool HigherIsBetter, string? AppliesToRole);

public record CreateEvaluationPeriodRequest(string Name, DateOnly StartDate, DateOnly EndDate);

public record KpiDefinitionResponse(
    long Id, string Code, string Name, string? Description, decimal Weight,
    decimal? TargetValue, string? Formula, bool HigherIsBetter, bool IsActive, string? AppliesToRole);

public record UpsertKpiDefinitionRequest(
    string Code, string Name, string? Description, decimal Weight,
    decimal? TargetValue, string? Formula, bool HigherIsBetter, bool IsActive,
    string? AppliesToRole = null);

public record EmployeeKpiResultResponse(
    long Id, long KpiDefinitionId, string KpiCode, string KpiName,
    decimal SnapshotWeight, decimal? SnapshotTarget, string SnapshotFormula, bool SnapshotHigherIsBetter,
    decimal CalculatedValue, decimal? AdjustedValue, decimal Score, string? Comment);

public record EmployeeEvaluationResponse(
    long Id, long PeriodId, string PeriodName, long UserId, string UserName, string Status,
    decimal? OverallScore, string? Notes, DateTimeOffset? SubmittedAt, DateTimeOffset? ReviewedAt,
    DateTimeOffset? ApprovedAt, string? RejectionReason, string? ReopenReason,
    IReadOnlyList<EmployeeKpiResultResponse> Results);

public record CreateTaskQualityRequest(long TaskId, int QualityScore, string? Notes);
public record TaskQualityEvaluationResponse(
    long Id, long TaskId, long EvaluatorId, int QualityScore, string? Notes, DateTimeOffset CreatedAt);

public record RejectEvaluationRequest(string Reason);
public record ReopenEvaluationRequest(string Reason);
public record AdjustKpiResultRequest(decimal? AdjustedValue, decimal? Score, string? Comment);
