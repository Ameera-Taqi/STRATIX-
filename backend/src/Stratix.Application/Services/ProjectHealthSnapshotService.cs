using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Health;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Progress;

namespace Stratix.Application.Services;

public interface IProjectHealthSnapshotService
{
    Task<ProjectHealthSnapshotResponse> CaptureAsync(long projectId, CancellationToken ct = default);
    Task<ProjectHealthSnapshotResponse?> GetLatestAsync(long projectId, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectHealthSnapshotResponse>> GetHistoryAsync(long projectId, int take = 30, CancellationToken ct = default);
    Task<IReadOnlyList<ProjectHealthSnapshotResponse>> GetDashboardAsync(CancellationToken ct = default);
}

public class ProjectHealthSnapshotService : IProjectHealthSnapshotService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public ProjectHealthSnapshotService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<ProjectHealthSnapshotResponse> CaptureAsync(long projectId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new KeyNotFoundException("Project not found");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var tasks = await _db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync(ct);
        var criticalRisks = await _db.ProjectRisks.CountAsync(
            r => r.ProjectId == projectId && r.Status != RiskStatus.CLOSED && r.RiskLevel == RiskLevel.CRITICAL, ct);

        var slices = tasks.Select(t => new TaskEffortSlice(
            t.Id, t.StageId, t.Status, t.EstimatedHours, t.DueDate, t.CompletedAt));

        var factors = ProjectHealthCalculator.BuildFactors(project.Progress, slices, criticalRisks, today);
        var result = ProjectHealthCalculator.Compute(factors);

        var snap = new ProjectHealthSnapshot
        {
            OrganizationId = project.OrganizationId,
            ProjectId = projectId,
            Score = result.Score,
            Status = result.Status,
            Progress = factors.Progress,
            OnTimeTasks = factors.OnTimeTasks,
            DelayedTasks = factors.DelayedTasks,
            CriticalRisks = factors.CriticalRisks,
            NoteKey = result.NoteKey,
            CapturedAt = DateTimeOffset.UtcNow,
        };
        _db.Add(snap);
        await _db.SaveChangesAsync(ct);

        return ToResponse(snap, project.Name);
    }

    public async Task<ProjectHealthSnapshotResponse?> GetLatestAsync(long projectId, CancellationToken ct = default)
    {
        var snap = await _db.ProjectHealthSnapshots
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CapturedAt)
            .FirstOrDefaultAsync(ct);
        if (snap is null) return null;
        var name = await _db.Projects.Where(p => p.Id == projectId).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "";
        return ToResponse(snap, name);
    }

    public async Task<IReadOnlyList<ProjectHealthSnapshotResponse>> GetHistoryAsync(long projectId, int take = 30, CancellationToken ct = default)
    {
        take = Math.Clamp(take, 1, 100);
        var name = await _db.Projects.Where(p => p.Id == projectId).Select(p => p.Name).FirstOrDefaultAsync(ct) ?? "";
        var rows = await _db.ProjectHealthSnapshots
            .Where(s => s.ProjectId == projectId)
            .OrderByDescending(s => s.CapturedAt)
            .Take(take)
            .ToListAsync(ct);
        return rows.Select(s => ToResponse(s, name)).ToList();
    }

    public async Task<IReadOnlyList<ProjectHealthSnapshotResponse>> GetDashboardAsync(CancellationToken ct = default)
    {
        var projects = RoleDataScope.Apply(_db.Projects, _currentUser.UserId, _currentUser.Role);
        var projectIds = await projects.Select(p => p.Id).ToListAsync(ct);
        if (projectIds.Count == 0) return [];

        var latestIds = await _db.ProjectHealthSnapshots
            .Where(s => projectIds.Contains(s.ProjectId))
            .GroupBy(s => s.ProjectId)
            .Select(g => g.Max(x => x.Id))
            .ToListAsync(ct);

        var latest = await _db.ProjectHealthSnapshots
            .Where(s => latestIds.Contains(s.Id))
            .ToListAsync(ct);

        var names = await _db.Projects.Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        return latest
            .Where(s => s is not null)
            .Select(s => ToResponse(s, names.GetValueOrDefault(s.ProjectId, "")))
            .ToList();
    }

    private static ProjectHealthSnapshotResponse ToResponse(ProjectHealthSnapshot s, string projectName) =>
        new(s.Id, s.ProjectId, projectName, s.Score, s.Status.ToString(), s.Progress,
            s.OnTimeTasks, s.DelayedTasks, s.CriticalRisks, s.NoteKey, s.CapturedAt);
}
