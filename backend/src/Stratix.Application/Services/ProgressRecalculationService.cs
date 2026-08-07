using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.Interfaces;
using Stratix.Domain.Enums;
using Stratix.Domain.Progress;

namespace Stratix.Application.Services;

public interface IProgressRecalculationService
{
    Task RecalculateProjectAsync(long projectId, CancellationToken ct = default);
    /// <summary>Backfill default effort on legacy rows and recompute progress for every project in scope.</summary>
    Task<int> RecalculateAllAsync(CancellationToken ct = default);
}

/// <summary>
/// Persists stage + project progress from <see cref="ProjectProgressCalculator"/> and may capture a health snapshot
/// only when factors change or once per UTC day (never on page read).
/// Soft-deleted tasks are excluded (ActiveOnly) from progress, on-time, delayed, and health score.
/// </summary>
public class ProgressRecalculationService : IProgressRecalculationService
{
    private readonly IApplicationDbContext _db;
    private readonly IProjectHealthSnapshotService _snapshots;

    public ProgressRecalculationService(IApplicationDbContext db, IProjectHealthSnapshotService snapshots)
    {
        _db = db;
        _snapshots = snapshots;
    }

    public async Task<int> RecalculateAllAsync(CancellationToken ct = default)
    {
        // Persist policy: any null/0 effort becomes 1 before weighting (active tasks only).
        var legacy = await _db.Tasks.ActiveOnly().Where(t => t.EstimatedHours <= 0).ToListAsync(ct);
        foreach (var task in legacy)
            task.EstimatedHours = ProjectProgressCalculator.DefaultEffortHours;
        if (legacy.Count > 0)
            await _db.SaveChangesAsync(ct);

        var projectIds = await _db.Projects.Select(p => p.Id).ToListAsync(ct);
        foreach (var id in projectIds)
            await RecalculateProjectAsync(id, ct);
        return projectIds.Count;
    }

    public async Task RecalculateProjectAsync(long projectId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is null) return;

        // Soft-deleted tasks never contribute to progress / health.
        var tasks = await _db.Tasks.ActiveOnly().Where(t => t.ProjectId == projectId).ToListAsync(ct);
        var stages = await _db.ProjectStages.Where(s => s.ProjectId == projectId).ToListAsync(ct);

        foreach (var task in tasks.Where(t => t.EstimatedHours <= 0))
            task.EstimatedHours = ProjectProgressCalculator.DefaultEffortHours;

        var slices = tasks.Select(t => new TaskEffortSlice(
            t.Id,
            t.StageId,
            t.Status,
            t.EstimatedHours,
            t.DueDate,
            t.CompletedAt)).ToList();

        // Always recompute every stage + the project together.
        // Task move Stage A → Stage B must refresh A, B, and project — not B alone.
        var result = ProjectProgressCalculator.ProjectProgressFromEffort(slices, stages.Select(s => s.Id));
        project.Progress = result.Progress;

        foreach (var stage in stages)
        {
            var stageResult = result.Stages.FirstOrDefault(s => s.StageId == stage.Id);
            // Empty stage → 0 (calculator); never leave stale manual %.
            stage.Progress = stageResult.StageId == stage.Id
                ? stageResult.Progress
                : ProjectProgressCalculator.StageProgressFromTasks(slices.Where(t => t.StageId == stage.Id));

            // Auto-activate stages that gained progress; do not auto-close (close rules enforce Complete).
            if (stage.Status == StageStatus.PLANNED && stage.Progress > 0)
                stage.Status = StageStatus.ACTIVE;
            // Reopened work: a DONE stage with incomplete tasks must leave DONE.
            else if (stage.Status == StageStatus.DONE && stage.Progress < 100)
                stage.Status = StageStatus.ACTIVE;
        }

        await _db.SaveChangesAsync(ct);
        // Not a formal analysis — skip identical same-day rows.
        await _snapshots.CaptureAsync(projectId, forceFormalCapture: false, ct);
    }
}
