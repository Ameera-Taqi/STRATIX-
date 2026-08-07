using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Tasks;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Events;
using Stratix.Domain.Progress;
using Stratix.Domain.Workflow;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class TaskService : ITaskService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;
    private readonly ICurrentUserService _currentUser;
    private readonly TenantRelationGuard _tenantGuard;
    private readonly IProgressRecalculationService _progress;
    private readonly IDomainEventDispatcher _events;

    public TaskService(
        IApplicationDbContext db,
        IAuditTrailService audit,
        ICurrentUserService currentUser,
        TenantRelationGuard tenantGuard,
        IProgressRecalculationService progress,
        IDomainEventDispatcher events)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
        _tenantGuard = tenantGuard;
        _progress = progress;
        _events = events;
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(long? projectId, CancellationToken ct = default)
    {
        var query = ScopedQuery();
        if (projectId.HasValue)
        {
            if (!await ScopedProjects().AnyAsync(p => p.Id == projectId, ct))
                throw new KeyNotFoundException("Project not found");
            query = query.Where(t => t.ProjectId == projectId);
        }
        return await query.OrderBy(t => t.DueDate).ThenBy(t => t.Title)
            .Select(t => EntityMappers.ToResponse(t)).ToListAsync(ct);
    }

    public async Task<Common.PagedResult<TaskResponse>> GetPagedAsync(long? projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var (p, size) = Common.PageQuery.Normalize(page, pageSize);
        var query = ScopedQuery();
        if (projectId.HasValue)
        {
            if (!await ScopedProjects().AnyAsync(x => x.Id == projectId, ct))
                throw new KeyNotFoundException("Project not found");
            query = query.Where(t => t.ProjectId == projectId);
        }
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(t => t.DueDate).ThenBy(t => t.Title)
            .Skip((p - 1) * size).Take(size)
            .Select(t => EntityMappers.ToResponse(t)).ToListAsync(ct);
        return new Common.PagedResult<TaskResponse>(items, total, p, size);
    }

    public async Task<TaskResponse> GetByIdAsync(long id, CancellationToken ct = default) =>
        EntityMappers.ToResponse(await FindScopedAsync(id, ct));

    public async Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var task = new TaskItem
        {
            ProjectId = request.ProjectId,
            StageId = request.StageId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Status = request.Status ?? DomainTaskStatus.TODO,
            Priority = request.Priority ?? TaskPriority.MEDIUM,
            AssigneeId = request.AssigneeId,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            EstimatedHours = request.EstimatedHours is > 0 ? request.EstimatedHours.Value : ProjectProgressCalculator.DefaultEffortHours,
            ActualHours = request.ActualHours,
            CreatedAt = now,
            UpdatedAt = now
        };
        await ValidateRelationsAsync(task, ct);
        _db.Add(task);
        await _db.SaveChangesAsync(ct);
        task = await FindAsync(task.Id, ct);
        await _progress.RecalculateProjectAsync(task.ProjectId, ct);
        await _audit.RecordCreateAsync(
            AuditEntityType.TASK,
            task.Id,
            task.Title,
            AuditSnapshot.Serialize(Snapshot(task)),
            $"Task created: {task.Title}",
            task.ProjectId,
            task.Project.Name,
            ct);
        return EntityMappers.ToResponse(await FindAsync(task.Id, ct));
    }

    public async Task<TaskResponse> UpdateAsync(long id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        var oldStatus = task.Status;
        var oldProjectId = task.ProjectId;
        var oldStageId = task.StageId;
        var oldValues = AuditSnapshot.Serialize(Snapshot(task));

        TaskTransitionRules.EnsureTransition(
            oldStatus, request.Status, request.BlockedReason, request.ReopenReason, request.ReviewReason);

        task.ProjectId = request.ProjectId;
        task.StageId = request.StageId;
        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.AssigneeId = request.AssigneeId;
        task.StartDate = request.StartDate;
        task.DueDate = request.DueDate;
        if (request.EstimatedHours is > 0) task.EstimatedHours = request.EstimatedHours.Value;
        if (request.ActualHours.HasValue) task.ActualHours = request.ActualHours;
        ApplyReasons(task, oldStatus, request.Status, request.BlockedReason, request.ReopenReason, request.ReviewReason);
        task.UpdatedAt = DateTimeOffset.UtcNow;
        ApplyCompletion(task, request.Status);
        await ValidateRelationsAsync(task, ct);
        await _db.SaveChangesAsync(ct);

        // Stage A → Stage B (or any task change): recompute ALL stages + project, not destination only.
        await _progress.RecalculateProjectAsync(task.ProjectId, ct);
        if (oldProjectId != task.ProjectId)
            await _progress.RecalculateProjectAsync(oldProjectId, ct);

        if (oldStatus != request.Status)
            await DispatchStatusEventAsync(task, oldStatus, request.Status, ct);

        var stageMoved = oldStageId != task.StageId;
        await _audit.RecordUpdateAsync(
            AuditEntityType.TASK,
            task.Id,
            task.Title,
            oldValues,
            AuditSnapshot.Serialize(Snapshot(task)),
            stageMoved
                ? $"Task updated (stage {oldStageId?.ToString() ?? "none"} → {task.StageId?.ToString() ?? "none"}): {task.Title}"
                : $"Task updated: {task.Title}",
            task.ProjectId,
            task.Project.Name,
            ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task<TaskResponse> UpdateStatusAsync(long id, UpdateTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);

        if (_currentUser.Role == UserRole.EMPLOYEE && task.AssigneeId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You can only update the status of tasks assigned to you.");

        var oldStatus = task.Status;
        TaskTransitionRules.EnsureTransition(
            oldStatus, request.Status, request.BlockedReason, request.ReopenReason, request.ReviewReason);

        var oldValues = AuditSnapshot.Serialize(new { Status = task.Status.ToString() });
        task.Status = request.Status;
        ApplyReasons(task, oldStatus, request.Status, request.BlockedReason, request.ReopenReason, request.ReviewReason);
        task.UpdatedAt = DateTimeOffset.UtcNow;
        ApplyCompletion(task, request.Status);
        await _db.SaveChangesAsync(ct);
        await _progress.RecalculateProjectAsync(task.ProjectId, ct);

        if (oldStatus != request.Status)
            await DispatchStatusEventAsync(task, oldStatus, request.Status, ct);

        await _audit.RecordUpdateAsync(
            AuditEntityType.TASK,
            task.Id,
            task.Title,
            oldValues,
            AuditSnapshot.Serialize(new { Status = task.Status.ToString() }),
            $"Task status → {request.Status}",
            task.ProjectId,
            task.Project?.Name,
            ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        var title = task.Title;
        var projectId = task.ProjectId;
        var projectName = task.Project.Name;
        var snapshot = AuditSnapshot.Serialize(Snapshot(task));

        // Soft-delete quality rows so KPI cannot retain scores for a deleted task.
        var quality = await _db.TaskQualityEvaluations.Where(q => q.TaskId == id).ToListAsync(ct);
        foreach (var row in quality)
            _db.Remove(row);

        _db.Remove(task); // soft-delete via DbContext policy
        await _db.SaveChangesAsync(ct);

        // Immediate recalc: Progress, stages, on-time/delayed, health score (soft-deleted excluded).
        await _progress.RecalculateProjectAsync(projectId, ct);
        await _audit.RecordDeleteAsync(AuditEntityType.TASK, id, title, snapshot, $"Task deleted: {title}", projectId, projectName, ct);
    }

    private async Task DispatchStatusEventAsync(TaskItem task, DomainTaskStatus oldStatus, DomainTaskStatus newStatus, CancellationToken ct)
    {
        await _events.DispatchAsync(new TaskStatusChangedEvent(
            task.OrganizationId,
            task.ProjectId,
            task.Project?.Name ?? "",
            task.Id,
            task.Title,
            task.AssigneeId,
            task.Project?.ProjectManagerId,
            oldStatus,
            newStatus,
            _currentUser.UserId ?? 0,
            DateTimeOffset.UtcNow), ct);
    }

    private static void ApplyReasons(
        TaskItem task,
        DomainTaskStatus from,
        DomainTaskStatus to,
        string? blockedReason,
        string? reopenReason,
        string? reviewReason)
    {
        if (to == DomainTaskStatus.BLOCKED)
            task.BlockedReason = blockedReason?.Trim();
        else if (from == DomainTaskStatus.BLOCKED)
            task.BlockedReason = null;

        if (from == DomainTaskStatus.DONE && to != DomainTaskStatus.DONE)
            task.ReopenReason = reopenReason?.Trim();

        if (to == DomainTaskStatus.REVIEW && !string.IsNullOrWhiteSpace(reviewReason))
            task.ReviewReason = reviewReason.Trim();
    }

    private static object Snapshot(TaskItem t) => new
    {
        t.ProjectId,
        t.StageId,
        t.Title,
        t.Description,
        Status = t.Status.ToString(),
        Priority = t.Priority.ToString(),
        t.AssigneeId,
        t.StartDate,
        t.DueDate,
        t.Progress,
        t.EstimatedHours,
        t.BlockedReason,
        t.ReopenReason,
        t.ReviewReason
    };

    private static void ApplyCompletion(TaskItem task, DomainTaskStatus status) =>
        TaskCompletionPolicy.ApplyStatus(task, status);

    private IQueryable<Project> ScopedProjects() =>
        RoleDataScope.Apply(_db.Projects, _currentUser.UserId, _currentUser.Role);

    private IQueryable<TaskItem> ScopedQuery() =>
        RoleDataScope.Apply(
            _db.Tasks.Include(t => t.Project).Include(t => t.Stage).Include(t => t.Assignee),
            _currentUser.UserId,
            _currentUser.Role);

    private IQueryable<TaskItem> Query() =>
        _db.Tasks.Include(t => t.Project).Include(t => t.Stage).Include(t => t.Assignee);

    private async Task<TaskItem> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new KeyNotFoundException("Task not found");

    private async Task<TaskItem> FindScopedAsync(long id, CancellationToken ct) =>
        await ScopedQuery().FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new KeyNotFoundException("Task not found");

    private async Task ValidateRelationsAsync(TaskItem task, CancellationToken ct)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == task.ProjectId, ct)
            ?? throw new ArgumentException("Project not found");

        task.OrganizationId = project.OrganizationId;

        await _tenantGuard.EnsureStageInProjectAsync(task.StageId, task.ProjectId, project.OrganizationId, ct);
        await _tenantGuard.EnsureUserAsync(task.AssigneeId, project.OrganizationId, ct);
    }
}
