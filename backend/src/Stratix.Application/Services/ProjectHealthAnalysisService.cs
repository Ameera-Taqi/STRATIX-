using Stratix.Application.DTOs.Ai;
using Stratix.Application.Interfaces;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class ProjectHealthAnalysisService : IProjectHealthAnalysisService
{
    public Task<ProjectHealthAnalysisResponse> AnalyzeAsync(ProjectHealthAnalysisRequest request, CancellationToken ct = default)
    {
        var progress = (int)request.Project.ProgressPercentage;
        var healthScore = (int)request.Performance.ProjectHealthScore;
        var healthStatus = healthScore >= 70 ? HealthClassification.HEALTHY :
            healthScore >= 45 ? HealthClassification.WARNING : HealthClassification.CRITICAL;
        var deliveryRisk = ClassifyDeliveryRisk(request);
        var concerns = BuildConcerns(request);
        var recommendations = BuildRecommendations(request, deliveryRisk);
        var insights = BuildInsights(request);
        var summary = $"Project \"{request.Project.Name}\" is {request.Project.Status} at {progress}% completion. " +
                      $"{request.Tasks.CompletedTasks}/{request.Tasks.TotalTasks} tasks done with {request.Risks.OpenRisks} open risks.";

        return Task.FromResult(new ProjectHealthAnalysisResponse(
            healthStatus.ToString(),
            deliveryRisk.ToString(),
            summary,
            concerns,
            recommendations,
            insights,
            "STRATIX_ENGINE"));
    }

    private static DeliveryRisk ClassifyDeliveryRisk(ProjectHealthAnalysisRequest request)
    {
        var riskScore = 0;
        if (request.Risks.CriticalRisks > 0) riskScore += 2;
        if (request.Risks.OpenRisks >= 4) riskScore += 1;
        if (request.Tasks.OverdueTasks >= 3) riskScore += 2;
        if (request.Tasks.DelayedTasks >= 2) riskScore += 1;
        if (request.Stages.DelayedStages >= 2) riskScore += 1;
        if (request.ChangeRequests.OpenChangeRequests >= 3) riskScore += 1;
        return riskScore >= 4 ? DeliveryRisk.HIGH : riskScore >= 2 ? DeliveryRisk.MEDIUM : DeliveryRisk.LOW;
    }

    private static List<string> BuildConcerns(ProjectHealthAnalysisRequest r)
    {
        var list = new List<string>();
        if (r.Risks.CriticalRisks > 0) list.Add($"{r.Risks.CriticalRisks} critical risk(s) require immediate attention.");
        if (r.Tasks.OverdueTasks > 0) list.Add($"{r.Tasks.OverdueTasks} task(s) are overdue.");
        if (r.Stages.DelayedStages > 0) list.Add($"{r.Stages.DelayedStages} stage(s) are behind schedule.");
        return list;
    }

    private static List<string> BuildRecommendations(ProjectHealthAnalysisRequest r, DeliveryRisk deliveryRisk)
    {
        var list = new List<string>();
        if (deliveryRisk == DeliveryRisk.HIGH) list.Add("Escalate to steering committee and freeze scope changes.");
        if (r.Risks.OpenRisks > 0) list.Add("Review open risks and assign mitigation owners this week.");
        if (r.Tasks.DelayedTasks > 0) list.Add("Re-prioritize delayed tasks and adjust sprint capacity.");
        return list;
    }

    private static List<string> BuildInsights(ProjectHealthAnalysisRequest r)
    {
        var list = new List<string>();
        if (r.Performance.TeamKpiScore >= 80) list.Add("Team KPI performance is strong.");
        if (r.Stages.CompletedStages == r.Stages.TotalStages && r.Stages.TotalStages > 0)
            list.Add("All stages completed — focus on closure activities.");
        return list;
    }
}
