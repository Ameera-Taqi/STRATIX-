using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Ai;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Progress;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Application.Services;

public class ProjectHealthAnalysisService : IProjectHealthAnalysisService
{
    private readonly IApplicationDbContext _db;

    public ProjectHealthAnalysisService(IApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ProjectHealthAnalysisResponse> AnalyzeAsync(ProjectHealthAnalysisRequest request, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var project = await _db.Projects
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Status,
                p.Progress,
                p.StartDate,
                p.EndDate,
                p.OrganizationId,
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Project not found");

        var tasks = await _db.Tasks.ActiveOnly()
            .Where(t => t.ProjectId == request.ProjectId)
            .Select(t => new
            {
                t.Id,
                t.StageId,
                t.Status,
                t.EstimatedHours,
                t.DueDate,
                t.CompletedAt,
            })
            .ToListAsync(ct);

        var riskRows = await _db.ProjectRisks
            .Where(r => r.ProjectId == request.ProjectId)
            .Select(r => new { r.Status, r.RiskLevel })
            .ToListAsync(ct);

        var stages = await _db.ProjectStages
            .Where(s => s.ProjectId == request.ProjectId)
            .Select(s => new { s.Status, s.EndDate })
            .ToListAsync(ct);

        var completedTasks = tasks.Count(t => t.Status == DomainTaskStatus.DONE);
        var overdueTasks = tasks.Count(t => t.DueDate is { } due && due < today && t.Status != DomainTaskStatus.DONE);
        var openRisks = riskRows.Where(r => r.Status != RiskStatus.CLOSED).ToList();
        var criticalRisks = openRisks.Count(r => r.RiskLevel == RiskLevel.CRITICAL);

        var distribution = new RiskSeverityDistributionDto(
            openRisks.Count(r => r.RiskLevel == RiskLevel.LOW),
            openRisks.Count(r => r.RiskLevel == RiskLevel.MEDIUM),
            openRisks.Count(r => r.RiskLevel == RiskLevel.HIGH),
            criticalRisks);

        var completedStages = stages.Count(s => s.Status == StageStatus.DONE);
        var delayedStages = stages.Count(s => s.EndDate is { } end && end < today && s.Status != StageStatus.DONE);
        var taskCompletionRate = tasks.Count == 0 ? 0m : Math.Round((decimal)completedTasks / tasks.Count * 100m, 2);

        var slices = tasks.Select(t => new TaskEffortSlice(
            t.Id, t.StageId, t.Status, t.EstimatedHours, t.DueDate, t.CompletedAt));
        var factors = ProjectHealthCalculator.BuildFactors(project.Progress, slices, criticalRisks, today);
        var health = ProjectHealthCalculator.Compute(factors);

        _db.Add(new ProjectHealthSnapshot
        {
            OrganizationId = project.OrganizationId,
            ProjectId = project.Id,
            Score = health.Score,
            Status = health.Status,
            Progress = factors.Progress,
            OnTimeTasks = factors.OnTimeTasks,
            DelayedTasks = factors.DelayedTasks,
            CriticalRisks = factors.CriticalRisks,
            NoteKey = health.NoteKey,
            CapturedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);

        var metrics = new ProjectHealthAnalysisRequestMetrics(
            new ProjectInfoDto(project.Name, project.Status.ToString(), project.Progress, project.StartDate, project.EndDate),
            new TaskMetricsDto(tasks.Count, completedTasks, overdueTasks, overdueTasks),
            new RiskMetricsDto(openRisks.Count, criticalRisks, distribution),
            new StageMetricsDto(stages.Count, completedStages, delayedStages),
            new PerformanceMetricsDto(taskCompletionRate, Math.Round(health.Score, 1)));

        return AnalyzeMetrics(metrics);
    }

    private static ProjectHealthAnalysisResponse AnalyzeMetrics(ProjectHealthAnalysisRequestMetrics request)
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

        return new ProjectHealthAnalysisResponse(
            healthStatus.ToString(),
            deliveryRisk.ToString(),
            summary,
            concerns,
            recommendations,
            insights,
            "STRATIX_ENGINE",
            DateTimeOffset.UtcNow);
    }

    private static DeliveryRisk ClassifyDeliveryRisk(ProjectHealthAnalysisRequestMetrics request)
    {
        var riskScore = 0;
        if (request.Risks.CriticalRisks > 0) riskScore += 2;
        if (request.Risks.OpenRisks >= 4) riskScore += 1;
        if (request.Tasks.OverdueTasks >= 3) riskScore += 2;
        if (request.Tasks.DelayedTasks >= 2) riskScore += 1;
        if (request.Stages.DelayedStages >= 2) riskScore += 1;
        return riskScore >= 4 ? DeliveryRisk.HIGH : riskScore >= 2 ? DeliveryRisk.MEDIUM : DeliveryRisk.LOW;
    }

    private static List<string> BuildConcerns(ProjectHealthAnalysisRequestMetrics r)
    {
        var list = new List<string>();
        if (r.Risks.CriticalRisks > 0) list.Add($"{r.Risks.CriticalRisks} critical risk(s) require immediate attention.");
        if (r.Tasks.OverdueTasks > 0) list.Add($"{r.Tasks.OverdueTasks} task(s) are overdue.");
        if (r.Stages.DelayedStages > 0) list.Add($"{r.Stages.DelayedStages} feature(s) are behind schedule.");
        return list;
    }

    private static List<string> BuildRecommendations(ProjectHealthAnalysisRequestMetrics r, DeliveryRisk deliveryRisk)
    {
        var list = new List<string>();
        if (deliveryRisk == DeliveryRisk.HIGH) list.Add("Escalate to steering committee and freeze scope changes.");
        if (r.Risks.OpenRisks > 0) list.Add("Review open risks and assign mitigation owners this week.");
        if (r.Tasks.DelayedTasks > 0) list.Add("Re-prioritize delayed tasks and adjust sprint capacity.");
        return list;
    }

    private static List<string> BuildInsights(ProjectHealthAnalysisRequestMetrics r)
    {
        var list = new List<string>();
        if (r.Performance.TeamKpiScore >= 80) list.Add("Team KPI performance is strong.");
        if (r.Stages.CompletedStages == r.Stages.TotalStages && r.Stages.TotalStages > 0)
            list.Add("All features completed — focus on closure activities.");
        return list;
    }

    private sealed record ProjectHealthAnalysisRequestMetrics(
        ProjectInfoDto Project,
        TaskMetricsDto Tasks,
        RiskMetricsDto Risks,
        StageMetricsDto Stages,
        PerformanceMetricsDto Performance);
}
