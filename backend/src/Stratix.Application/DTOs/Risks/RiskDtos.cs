using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.Risks;

public record CreateRiskRequest(string Title, string? Description, RiskImpact Impact, RiskProbability Probability, string? MitigationPlan, RiskStatus? Status, long ProjectId, long OwnerId);
public record UpdateRiskRequest(string Title, string? Description, RiskImpact Impact, RiskProbability Probability, string? MitigationPlan, RiskStatus Status, long ProjectId, long OwnerId);
public record RiskResponse(long Id, string Title, string? Description, RiskImpact Impact, RiskProbability Probability, RiskLevel RiskLevel, string? MitigationPlan, RiskStatus Status, long ProjectId, string ProjectName, long OwnerId, string OwnerName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record RiskDashboardStatsResponse(int TotalRisks, int OpenRisks, int CriticalRisks, int ClosedRisks);
public record RiskHeatMapCell(RiskImpact Impact, RiskProbability Probability, RiskLevel RiskLevel, int Count);
public record RiskHeatMapResponse(IReadOnlyList<RiskHeatMapCell> Cells, int TotalRisks);
