namespace Stratix.Application.DTOs.Ai;

public record ProjectInfoDto(string Name, string Status, decimal ProgressPercentage, DateOnly? StartDate, DateOnly? EndDate);
public record TaskMetricsDto(int TotalTasks, int CompletedTasks, int DelayedTasks, int OverdueTasks);
public record RiskSeverityDistributionDto(int Low, int Medium, int High, int Critical);
public record RiskMetricsDto(int OpenRisks, int CriticalRisks, RiskSeverityDistributionDto SeverityDistribution);
public record StageMetricsDto(int TotalStages, int CompletedStages, int DelayedStages);
public record ChangeRequestMetricsDto(int OpenChangeRequests, int ApprovedChangeRequests);
public record PerformanceMetricsDto(decimal TeamKpiScore, decimal ProjectHealthScore);
public record ProjectHealthAnalysisRequest(ProjectInfoDto Project, TaskMetricsDto Tasks, RiskMetricsDto Risks, StageMetricsDto Stages, ChangeRequestMetricsDto ChangeRequests, PerformanceMetricsDto Performance);
public record ProjectHealthAnalysisResponse(string HealthStatus, string DeliveryRisk, string ExecutiveSummary, IReadOnlyList<string> MainConcerns, IReadOnlyList<string> Recommendations, IReadOnlyList<string> ManagementInsights, string AnalysisEngine);
