using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Events;
using Stratix.Domain.Workflow;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;
    private readonly IPlanLimitService _planLimits;
    private readonly TenantRelationGuard _tenantGuard;
    private readonly ICurrentUserService _currentUser;
    private readonly IProgressRecalculationService _progress;
    private readonly IDomainEventDispatcher _events;

    public ProjectService(
        IApplicationDbContext db,
        IAuditTrailService audit,
        IPlanLimitService planLimits,
        TenantRelationGuard tenantGuard,
        ICurrentUserService currentUser,
        IProgressRecalculationService progress,
        IDomainEventDispatcher events)
    {
        _db = db;
        _audit = audit;
        _planLimits = planLimits;
        _tenantGuard = tenantGuard;
        _currentUser = currentUser;
        _progress = progress;
        _events = events;
    }

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken ct = default) =>
        await ScopedQuery().OrderBy(p => p.Name).Select(p => EntityMappers.ToResponse(p)).ToListAsync(ct);

    public async Task<Common.PagedResult<ProjectResponse>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var (p, size) = Common.PageQuery.Normalize(page, pageSize);
        var query = ScopedQuery();
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(x => x.Name)
            .Skip((p - 1) * size).Take(size)
            .Select(x => EntityMappers.ToResponse(x)).ToListAsync(ct);
        return new Common.PagedResult<ProjectResponse>(items, total, p, size);
    }

    public async Task<ProjectResponse> GetByIdAsync(long id, CancellationToken ct = default) =>
        EntityMappers.ToResponse(await FindScopedAsync(id, ct));

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        await _planLimits.EnsureCanAddProjectAsync(ct);
        var orgId = _tenantGuard.ResolveOrganizationId(0);
        await _tenantGuard.EnsureDepartmentAsync(request.DepartmentId, orgId, ct);
        await _tenantGuard.EnsureUserAsync(request.ProjectManagerId, orgId, ct);

        var project = new Project
        {
            OrganizationId = orgId,
            Name = request.Name.Trim(),
            Description = request.Description,
            DepartmentId = request.DepartmentId,
            ProjectManagerId = request.ProjectManagerId,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status ?? ProjectStatus.PLANNED,
            Priority = request.Priority ?? ProjectPriority.MEDIUM,
            Progress = request.Progress ?? 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Add(project);
        await _db.SaveChangesAsync(ct);
        await LoadNavigationsAsync(project, ct);
        await _audit.RecordCreateAsync(
            AuditEntityType.PROJECT,
            project.Id,
            project.Name,
            AuditSnapshot.Serialize(Snapshot(project)),
            $"Project created: {project.Name}",
            project.Id,
            project.Name,
            ct);
        return EntityMappers.ToResponse(project);
    }

    public async Task<ProjectResponse> UpdateAsync(long id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await FindAsync(id, ct);
        await _tenantGuard.EnsureDepartmentAsync(request.DepartmentId, project.OrganizationId, ct);
        await _tenantGuard.EnsureUserAsync(request.ProjectManagerId, project.OrganizationId, ct);

        var oldValues = AuditSnapshot.Serialize(Snapshot(project));
        project.Name = request.Name.Trim();
        project.Description = request.Description;
        project.DepartmentId = request.DepartmentId;
        project.ProjectManagerId = request.ProjectManagerId;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = request.Status;
        project.Priority = request.Priority;
        // Progress is derived from task effort — only allow manual seed when no tasks.
        var hasTasks = await _db.Tasks.AnyAsync(t => t.ProjectId == id, ct);
        if (!hasTasks && request.Progress.HasValue)
            project.Progress = request.Progress.Value;

        if (request.Status == ProjectStatus.COMPLETED)
        {
            var stageStatuses = await _db.ProjectStages.Where(s => s.ProjectId == id).Select(s => s.Status).ToListAsync(ct);
            var taskStatuses = await _db.Tasks.Where(t => t.ProjectId == id).Select(t => t.Status).ToListAsync(ct);
            ProjectCloseRules.EnsureCanComplete(stageStatuses, taskStatuses);
        }

        await _db.SaveChangesAsync(ct);
        if (hasTasks)
            await _progress.RecalculateProjectAsync(id, ct);

        if (request.Status == ProjectStatus.COMPLETED)
        {
            await _events.DispatchAsync(new ProjectCompletedEvent(
                project.OrganizationId,
                project.Id,
                project.Name,
                project.ProjectManagerId,
                _currentUser.UserId ?? 0,
                DateTimeOffset.UtcNow), ct);
        }

        await _audit.RecordUpdateAsync(
            AuditEntityType.PROJECT,
            project.Id,
            project.Name,
            oldValues,
            AuditSnapshot.Serialize(Snapshot(project)),
            $"Project updated: {project.Name}",
            project.Id,
            project.Name,
            ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task<ProjectResponse> CompleteAsync(long id, CancellationToken ct = default)
    {
        var project = await FindScopedAsync(id, ct);
        var stageStatuses = await _db.ProjectStages.Where(s => s.ProjectId == id).Select(s => s.Status).ToListAsync(ct);
        var taskStatuses = await _db.Tasks.Where(t => t.ProjectId == id).Select(t => t.Status).ToListAsync(ct);
        ProjectCloseRules.EnsureCanComplete(stageStatuses, taskStatuses);

        project.Status = ProjectStatus.COMPLETED;
        project.Progress = 100;
        await _db.SaveChangesAsync(ct);
        await _events.DispatchAsync(new ProjectCompletedEvent(
            project.OrganizationId,
            project.Id,
            project.Name,
            project.ProjectManagerId,
            _currentUser.UserId ?? 0,
            DateTimeOffset.UtcNow), ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException("Project not found");

        var tasks = await _db.Tasks.Where(t => t.ProjectId == id).ToListAsync(ct);
        var taskIds = tasks.Select(t => t.Id).ToList();
        if (taskIds.Count > 0)
        {
            var comments = await _db.TaskComments.Where(c => taskIds.Contains(c.TaskId)).ToListAsync(ct);
            foreach (var comment in comments) _db.Remove(comment);
        }
        foreach (var task in tasks) _db.Remove(task);

        var stages = await _db.ProjectStages.Where(s => s.ProjectId == id).ToListAsync(ct);
        foreach (var stage in stages) _db.Remove(stage);

        var risks = await _db.ProjectRisks.Where(r => r.ProjectId == id).ToListAsync(ct);
        foreach (var risk in risks) _db.Remove(risk);

        var files = await _db.ProjectFiles.Where(f => f.ProjectId == id).ToListAsync(ct);
        foreach (var file in files) _db.Remove(file);

        var snapshot = AuditSnapshot.Serialize(Snapshot(project));
        _db.Remove(project);
        await _db.SaveChangesAsync(ct);
        await _audit.RecordDeleteAsync(
            AuditEntityType.PROJECT,
            id,
            project.Name,
            snapshot,
            $"Project deleted: {project.Name}",
            id,
            project.Name,
            ct);
    }

    private static object Snapshot(Project p) => new
    {
        p.Name,
        p.Description,
        p.DepartmentId,
        p.ProjectManagerId,
        p.StartDate,
        p.EndDate,
        Status = p.Status.ToString(),
        Priority = p.Priority.ToString(),
        p.Progress
    };

    private IQueryable<Project> Query() =>
        _db.Projects.Include(p => p.Department).Include(p => p.ProjectManager);

    private IQueryable<Project> ScopedQuery() =>
        RoleDataScope.Apply(Query(), _currentUser.UserId, _currentUser.Role);

    private async Task<Project> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new KeyNotFoundException("Project not found");

    private async Task<Project> FindScopedAsync(long id, CancellationToken ct) =>
        await ScopedQuery().FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new KeyNotFoundException("Project not found");

    private async Task LoadNavigationsAsync(Project project, CancellationToken ct) =>
        await Query().Where(p => p.Id == project.Id).LoadAsync(ct);
}
