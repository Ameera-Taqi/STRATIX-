using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;

    public ProjectService(IApplicationDbContext db, IAuditTrailService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken ct = default) =>
        await Query().OrderBy(p => p.Name).Select(p => EntityMappers.ToResponse(p)).ToListAsync(ct);

    public async Task<ProjectResponse> GetByIdAsync(long id, CancellationToken ct = default) =>
        EntityMappers.ToResponse(await FindAsync(id, ct));

    public async Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default)
    {
        var project = new Project
        {
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
        await _audit.RecordCreateAsync(AuditEntityType.PROJECT, project.Id, project.Name, null, $"Project created: {project.Name}", project.Id, project.Name, ct);
        return EntityMappers.ToResponse(project);
    }

    public async Task<ProjectResponse> UpdateAsync(long id, UpdateProjectRequest request, CancellationToken ct = default)
    {
        var project = await FindAsync(id, ct);
        project.Name = request.Name.Trim();
        project.Description = request.Description;
        project.DepartmentId = request.DepartmentId;
        project.ProjectManagerId = request.ProjectManagerId;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.Status = request.Status;
        project.Priority = request.Priority;
        if (request.Progress.HasValue) project.Progress = request.Progress.Value;
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(project);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct)
            ?? throw new KeyNotFoundException("Project not found");

        var tasks = await _db.Tasks.Where(t => t.ProjectId == id).ToListAsync(ct);
        foreach (var task in tasks) _db.Remove(task);

        var stages = await _db.ProjectStages.Where(s => s.ProjectId == id).ToListAsync(ct);
        foreach (var stage in stages) _db.Remove(stage);

        var risks = await _db.ProjectRisks.Where(r => r.ProjectId == id).ToListAsync(ct);
        foreach (var risk in risks) _db.Remove(risk);

        _db.Remove(project);
        await _db.SaveChangesAsync(ct);
        await _audit.RecordDeleteAsync(AuditEntityType.PROJECT, id, project.Name, null, $"Project deleted: {project.Name}", id, project.Name, ct);
    }

    private IQueryable<Project> Query() =>
        _db.Projects.Include(p => p.Department).Include(p => p.ProjectManager);

    private async Task<Project> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new KeyNotFoundException("Project not found");

    private async Task LoadNavigationsAsync(Project project, CancellationToken ct) =>
        await Query().Where(p => p.Id == project.Id).LoadAsync(ct);
}
