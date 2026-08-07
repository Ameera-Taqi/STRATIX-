using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Stages;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Events;
using Stratix.Domain.Workflow;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Application.Services;

public class StageService : IStageService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;
    private readonly IProgressRecalculationService _progress;
    private readonly IDomainEventDispatcher _events;
    private readonly ICurrentUserService _currentUser;

    public StageService(
        IApplicationDbContext db,
        IAuditTrailService audit,
        IProgressRecalculationService progress,
        IDomainEventDispatcher events,
        ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _progress = progress;
        _events = events;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<StageResponse>> GetByProjectAsync(long projectId, CancellationToken ct = default)
    {
        await EnsureProjectExistsAsync(projectId, ct);
        return await _db.ProjectStages.Where(s => s.ProjectId == projectId)
            .OrderBy(s => s.OrderNumber)
            .Select(s => EntityMappers.ToResponse(s))
            .ToListAsync(ct);
    }

    public async Task<StageResponse> CreateAsync(long projectId, CreateStageRequest request, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct)
            ?? throw new KeyNotFoundException("Project not found");
        var order = request.OrderNumber ?? await _db.ProjectStages.Where(s => s.ProjectId == projectId).CountAsync(ct) + 1;
        var now = DateTimeOffset.UtcNow;
        var stage = new ProjectStage
        {
            OrganizationId = project.OrganizationId,
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status ?? StageStatus.PLANNED,
            // Empty stage policy: progress is task-derived; new stage starts at 0.
            Progress = 0,
            OrderNumber = order,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Add(stage);
        await _db.SaveChangesAsync(ct);
        await _audit.RecordCreateAsync(AuditEntityType.STAGE, stage.Id, stage.Name, null, $"Feature created: {stage.Name}", projectId, project.Name, ct);
        return EntityMappers.ToResponse(stage);
    }

    public async Task<StageResponse> UpdateAsync(long id, UpdateStageRequest request, CancellationToken ct = default)
    {
        var stage = await FindAsync(id, ct);
        stage.Name = request.Name.Trim();
        stage.Description = request.Description;
        stage.StartDate = request.StartDate;
        stage.EndDate = request.EndDate;
        stage.Status = request.Status;
        // Progress is always derived from tasks (empty stage → 0). Ignore client Progress writes.
        stage.OrderNumber = request.OrderNumber;
        stage.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _progress.RecalculateProjectAsync(stage.ProjectId, ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task<StageResponse> CompleteAsync(long id, CancellationToken ct = default)
    {
        var stage = await FindAsync(id, ct);
        var tasks = await _db.Tasks.Where(t => t.StageId == id)
            .Select(t => new { t.Status, t.Title })
            .ToListAsync(ct);
        StageCloseRules.EnsureCanComplete(tasks.Select(t => ((DomainTaskStatus)t.Status, t.Title)));

        stage.Status = StageStatus.DONE;
        // Do not set Progress here — recalc applies empty→0 / all-done→100.
        stage.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        await _progress.RecalculateProjectAsync(stage.ProjectId, ct);

        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == stage.ProjectId, ct);
        await _events.DispatchAsync(new StageCompletedEvent(
            stage.OrganizationId,
            stage.ProjectId,
            project?.Name ?? "",
            stage.Id,
            stage.Name,
            project?.ProjectManagerId,
            _currentUser.UserId ?? 0,
            _currentUser.UserName ?? "",
            DateTimeOffset.UtcNow), ct);

        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var stage = await FindAsync(id, ct);
        var projectId = stage.ProjectId;
        _db.Remove(stage);
        await _db.SaveChangesAsync(ct);
        await _progress.RecalculateProjectAsync(projectId, ct);
    }

    public async Task<IReadOnlyList<StageResponse>> MoveAsync(long id, string direction, CancellationToken ct = default)
    {
        var stage = await FindAsync(id, ct);
        var siblings = await _db.ProjectStages
            .Where(s => s.ProjectId == stage.ProjectId)
            .OrderBy(s => s.OrderNumber)
            .ToListAsync(ct);

        var idx = siblings.FindIndex(s => s.Id == id);
        if (idx < 0) return siblings.Select(EntityMappers.ToResponse).ToList();

        var dir = (direction ?? "").Trim().ToLowerInvariant();
        // Display list reorder only — not a schedule dependency ("earlier"/"later" are not accepted).
        var swapIdx = dir is "up" ? idx - 1
            : dir is "down" ? idx + 1
            : -1;
        if (swapIdx < 0 || swapIdx >= siblings.Count)
            return siblings.Select(EntityMappers.ToResponse).ToList();

        var other = siblings[swapIdx];
        var a = stage.OrderNumber;
        var b = other.OrderNumber;
        // Unique (project_id, order_number) — park one row on a temp value first.
        var temp = siblings.Max(s => s.OrderNumber) + 1000;
        stage.OrderNumber = temp;
        stage.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        other.OrderNumber = a;
        other.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        stage.OrderNumber = b;
        stage.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetByProjectAsync(stage.ProjectId, ct);
    }

    private async Task<ProjectStage> FindAsync(long id, CancellationToken ct) =>
        await _db.ProjectStages.FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new KeyNotFoundException("Feature not found");

    private async Task EnsureProjectExistsAsync(long projectId, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct))
            throw new KeyNotFoundException("Project not found");
    }
}
