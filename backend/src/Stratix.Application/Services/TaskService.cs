using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Tasks;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class TaskService : ITaskService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;
    private readonly ICurrentUserService _currentUser;

    public TaskService(IApplicationDbContext db, IAuditTrailService audit, ICurrentUserService currentUser)
    {
        _db = db;
        _audit = audit;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<TaskResponse>> GetAllAsync(long? projectId, CancellationToken ct = default)
    {
        var query = Query();
        if (projectId.HasValue)
        {
            if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct))
                throw new KeyNotFoundException("Project not found");
            query = query.Where(t => t.ProjectId == projectId);
        }
        return await query.OrderBy(t => t.DueDate).ThenBy(t => t.Title)
            .Select(t => EntityMappers.ToResponse(t)).ToListAsync(ct);
    }

    public async Task<Common.PagedResult<TaskResponse>> GetPagedAsync(long? projectId, int page, int pageSize, CancellationToken ct = default)
    {
        var (p, size) = Common.PageQuery.Normalize(page, pageSize);
        var query = Query();
        if (projectId.HasValue)
        {
            if (!await _db.Projects.AnyAsync(x => x.Id == projectId, ct))
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
        EntityMappers.ToResponse(await FindAsync(id, ct));

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
            CreatedAt = now,
            UpdatedAt = now
        };
        await ValidateRelationsAsync(task, ct);
        _db.Add(task);
        await _db.SaveChangesAsync(ct);
        task = await FindAsync(task.Id, ct);
        await _audit.RecordCreateAsync(AuditEntityType.TASK, task.Id, task.Title, null, $"Task created: {task.Title}", task.ProjectId, task.Project.Name, ct);
        return EntityMappers.ToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(long id, UpdateTaskRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        task.ProjectId = request.ProjectId;
        task.StageId = request.StageId;
        task.Title = request.Title.Trim();
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.AssigneeId = request.AssigneeId;
        task.StartDate = request.StartDate;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        ApplyCompletion(task, request.Status);
        await ValidateRelationsAsync(task, ct);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task<TaskResponse> UpdateStatusAsync(long id, UpdateTaskStatusRequest request, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);

        // An EMPLOYEE may only move the status of tasks assigned to them.
        if (_currentUser.Role == UserRole.EMPLOYEE && task.AssigneeId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You can only update the status of tasks assigned to you.");

        task.Status = request.Status;
        task.UpdatedAt = DateTimeOffset.UtcNow;
        ApplyCompletion(task, request.Status);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(task);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var task = await FindAsync(id, ct);
        var title = task.Title;
        var projectId = task.ProjectId;
        var projectName = task.Project.Name;
        _db.Remove(task);
        await _db.SaveChangesAsync(ct);
        await _audit.RecordDeleteAsync(AuditEntityType.TASK, id, title, null, $"Task deleted: {title}", projectId, projectName, ct);
    }

    private static void ApplyCompletion(TaskItem task, DomainTaskStatus status)
    {
        if (status == DomainTaskStatus.DONE)
        {
            task.CompletedAt ??= DateTimeOffset.UtcNow;
            task.Progress = 100;
        }
        else
        {
            task.CompletedAt = null;
        }
    }

    private IQueryable<TaskItem> Query() =>
        _db.Tasks.Include(t => t.Project).Include(t => t.Stage).Include(t => t.Assignee);

    private async Task<TaskItem> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(t => t.Id == id, ct)
        ?? throw new KeyNotFoundException("Task not found");

    private async Task ValidateRelationsAsync(TaskItem task, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == task.ProjectId, ct))
            throw new ArgumentException("Project not found");

        if (task.StageId.HasValue)
        {
            var stage = await _db.ProjectStages.FirstOrDefaultAsync(s => s.Id == task.StageId, ct)
                ?? throw new ArgumentException("Stage not found");
            if (stage.ProjectId != task.ProjectId)
                throw new ArgumentException("Stage does not belong to project");
        }

        if (task.AssigneeId.HasValue && !await _db.Users.AnyAsync(u => u.Id == task.AssigneeId, ct))
            throw new ArgumentException("Assignee not found");
    }
}
