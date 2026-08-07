namespace Stratix.Application.DTOs.Kpi;

public record EvaluationPeriodResponse(
    long Id, string Name, DateOnly StartDate, DateOnly EndDate, string Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record CreateEvaluationPeriodRequest(string Name, DateOnly StartDate, DateOnly EndDate);

public record KpiDefinitionResponse(
    long Id, string Code, string Name, string? Description, decimal Weight, bool HigherIsBetter, bool IsActive);

public record UpsertKpiDefinitionRequest(
    string Code, string Name, string? Description, decimal Weight, bool HigherIsBetter, bool IsActive);

public record EmployeeKpiResultResponse(
    long Id, long KpiDefinitionId, string KpiCode, string KpiName,
    decimal CalculatedValue, decimal? AdjustedValue, decimal Score, string? Comment);

public record EmployeeEvaluationResponse(
    long Id, long PeriodId, string PeriodName, long UserId, string UserName, string Status,
    decimal? OverallScore, string? Notes, DateTimeOffset? SubmittedAt, DateTimeOffset? ReviewedAt,
    DateTimeOffset? ApprovedAt, string? RejectionReason, IReadOnlyList<EmployeeKpiResultResponse> Results);

public record CreateTaskQualityRequest(long TaskId, int QualityScore, string? Notes);
public record TaskQualityEvaluationResponse(
    long Id, long TaskId, long EvaluatorId, int QualityScore, string? Notes, DateTimeOffset CreatedAt);

public record RejectEvaluationRequest(string Reason);
