using Microsoft.EntityFrameworkCore;
using Stratix.Application.Interfaces;
using Stratix.Domain.Enums;
using Stratix.Domain.Progress;

namespace Stratix.Application.Services;

public interface IProgressRecalculationService
{
    Task RecalculateProjectAsync(long projectId, CancellationToken ct = default);
}

/// <summary>Persists stage + project progress from <see cref="ProjectProgressCalculator"/> and captures a health snapshot.</summary>
public class ProgressRecalculationService : IProgressRecalculationService
{
    private readonly IApplicationDbContext _db;
    private readonly IProjectHealthSnapshotService _snapshots;

    public ProgressRecalculationService(IApplicationDbContext db, IProjectHealthSnapshotService snapshots)
    {
        _db = db;
        _snapshots = snapshots;
    }

    public async Task RecalculateProjectAsync(long projectId, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        if (project is null) return;

        var tasks = await _db.Tasks.Where(t => t.ProjectId == projectId).ToListAsync(ct);
        var stages = await _db.ProjectStages.Where(s => s.ProjectId == projectId).ToListAsync(ct);

        var slices = tasks.Select(t => new TaskEffortSlice(
            t.Id,
            t.StageId,
            t.Status,
            t.EstimatedHours,
            t.DueDate,
            t.CompletedAt)).ToList();

        var result = ProjectProgressCalculator.ProjectProgressFromEffort(slices, stages.Select(s => s.Id));
        project.Progress = result.Progress;

        foreach (var stage in stages)
        {
            var stageResult = result.Stages.FirstOrDefault(s => s.StageId == stage.Id);
            stage.Progress = stageResult.StageId == stage.Id
                ? stageResult.Progress
                : ProjectProgressCalculator.StageProgressFromTasks(slices.Where(t => t.StageId == stage.Id));

            // Auto-activate stages that gained progress; do not auto-close (close rules enforce Complete).
            if (stage.Status == StageStatus.PLANNED && stage.Progress > 0)
                stage.Status = StageStatus.ACTIVE;
        }

        await _db.SaveChangesAsync(ct);
        await _snapshots.CaptureAsync(projectId, ct);
    }
}
