using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Stages;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class StageService : IStageService
{
    private readonly IApplicationDbContext _db;
    private readonly IAuditTrailService _audit;

    public StageService(IApplicationDbContext db, IAuditTrailService audit)
    {
        _db = db;
        _audit = audit;
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
        await EnsureProjectExistsAsync(projectId, ct);
        var order = request.OrderNumber ?? await _db.ProjectStages.Where(s => s.ProjectId == projectId).CountAsync(ct) + 1;
        var now = DateTimeOffset.UtcNow;
        var stage = new ProjectStage
        {
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = request.Status ?? StageStatus.PLANNED,
            Progress = request.Progress ?? 0,
            OrderNumber = order,
            CreatedAt = now,
            UpdatedAt = now
        };
        _db.Add(stage);
        await _db.SaveChangesAsync(ct);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        await _audit.RecordCreateAsync(AuditEntityType.STAGE, stage.Id, stage.Name, null, $"Stage created: {stage.Name}", projectId, project?.Name, ct);
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
        if (request.Progress.HasValue) stage.Progress = request.Progress.Value;
        stage.OrderNumber = request.OrderNumber;
        stage.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(stage);
    }

    public async Task<StageResponse> CompleteAsync(long id, CancellationToken ct = default)
    {
        var stage = await FindAsync(id, ct);
        stage.Status = StageStatus.DONE;
        stage.Progress = 100;
        stage.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(stage);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var stage = await FindAsync(id, ct);
        _db.Remove(stage);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<ProjectStage> FindAsync(long id, CancellationToken ct) =>
        await _db.ProjectStages.FirstOrDefaultAsync(s => s.Id == id, ct)
        ?? throw new KeyNotFoundException("Stage not found");

    private async Task EnsureProjectExistsAsync(long projectId, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct))
            throw new KeyNotFoundException("Project not found");
    }
}
