using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Milestones;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class MilestoneService : IMilestoneService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;

    public MilestoneService(IApplicationDbContext db, IAuditTrailService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<IReadOnlyList<MilestoneResponse>> GetAllAsync(CancellationToken ct = default) =>
        await Query().OrderBy(m => m.DueDate).Select(m => EntityMappers.ToResponse(m)).ToListAsync(ct);

    public async Task<IReadOnlyList<MilestoneResponse>> GetByProjectAsync(long projectId, CancellationToken ct = default)
    {
        await EnsureProjectExistsAsync(projectId, ct);
        return await Query().Where(m => m.ProjectId == projectId).OrderBy(m => m.DueDate)
            .Select(m => EntityMappers.ToResponse(m)).ToListAsync(ct);
    }

    public async Task<MilestoneResponse> GetByIdAsync(long id, CancellationToken ct = default) =>
        EntityMappers.ToResponse(await FindAsync(id, ct));

    public async Task<MilestoneResponse> CreateAsync(CreateMilestoneRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var milestone = new Milestone
        {
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            DueDate = request.DueDate,
            CompletedDate = request.CompletedDate,
            Status = request.Status ?? MilestoneStatus.PENDING,
            CreatedAt = now,
            UpdatedAt = now
        };
        await ValidateRelationsAsync(milestone, ct);
        _db.Add(milestone);
        await _db.SaveChangesAsync(ct);
        milestone = await FindAsync(milestone.Id, ct);
        await _audit.RecordCreateAsync(AuditEntityType.MILESTONE, milestone.Id, milestone.Title, null,
            $"Milestone created: {milestone.Title}", milestone.ProjectId, milestone.Project!.Name, ct);
        return EntityMappers.ToResponse(milestone);
    }

    public async Task<MilestoneResponse> UpdateAsync(long id, UpdateMilestoneRequest request, CancellationToken ct = default)
    {
        var milestone = await FindAsync(id, ct);
        milestone.ProjectId = request.ProjectId;
        milestone.Title = request.Title.Trim();
        milestone.DueDate = request.DueDate;
        milestone.CompletedDate = request.CompletedDate;
        milestone.Status = request.Status;
        milestone.UpdatedAt = DateTimeOffset.UtcNow;
        await ValidateRelationsAsync(milestone, ct);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var milestone = await FindAsync(id, ct);
        var (projectId, projectName, title) = (milestone.ProjectId, milestone.Project!.Name, milestone.Title);
        _db.Remove(milestone);
        await _db.SaveChangesAsync(ct);
        await _audit.RecordDeleteAsync(AuditEntityType.MILESTONE, id, title, null, $"Milestone deleted: {title}", projectId, projectName, ct);
    }

    private IQueryable<Milestone> Query() => _db.Milestones.Include(m => m.Project);

    private async Task<Milestone> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(m => m.Id == id, ct)
        ?? throw new KeyNotFoundException("Milestone not found");

    private async Task ValidateRelationsAsync(Milestone milestone, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == milestone.ProjectId, ct))
            throw new ArgumentException("Project not found");
    }

    private async Task EnsureProjectExistsAsync(long projectId, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct))
            throw new KeyNotFoundException("Project not found");
    }
}
