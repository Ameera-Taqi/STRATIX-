using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.ChangeRequests;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class ChangeRequestService : IChangeRequestService
{
    private readonly IApplicationDbContext _db;

    public ChangeRequestService(IApplicationDbContext db) => _db = db;

    private IQueryable<ChangeRequest> Query() =>
        _db.ChangeRequests.Include(c => c.Project).Include(c => c.RequestedBy).Include(c => c.ReviewedBy);

    public async Task<IReadOnlyList<ChangeRequestResponse>> GetAllAsync(long? projectId, CancellationToken ct = default)
    {
        var q = Query();
        if (projectId.HasValue)
        {
            if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct))
                throw new KeyNotFoundException("Project not found");
            q = q.Where(c => c.ProjectId == projectId);
        }
        return await q.OrderByDescending(c => c.CreatedAt).Select(c => EntityMappers.ToResponse(c)).ToListAsync(ct);
    }

    public async Task<ChangeRequestResponse> GetByIdAsync(long id, CancellationToken ct = default) =>
        EntityMappers.ToResponse(await FindAsync(id, ct));

    public async Task<ChangeRequestResponse> CreateAsync(CreateChangeRequestRequest request, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var entity = new ChangeRequest
        {
            ProjectId = request.ProjectId,
            Title = request.Title.Trim(),
            Description = request.Description,
            Priority = request.Priority ?? TaskPriority.MEDIUM,
            Status = ChangeRequestStatus.PENDING,
            RequestedById = request.RequestedById,
            CreatedAt = now,
            UpdatedAt = now
        };
        await ValidateAsync(entity.ProjectId, entity.RequestedById, ct);
        _db.Add(entity);
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await FindAsync(entity.Id, ct));
    }

    public async Task<ChangeRequestResponse> UpdateAsync(long id, UpdateChangeRequestRequest request, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        entity.Title = request.Title.Trim();
        entity.Description = request.Description;
        entity.Status = request.Status;
        entity.Priority = request.Priority;
        entity.ReviewedById = request.ReviewedById;
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        if (request.ReviewedById is { } reviewer && !await _db.Users.AnyAsync(u => u.Id == reviewer, ct))
            throw new ArgumentException("Reviewer not found");
        await _db.SaveChangesAsync(ct);
        return EntityMappers.ToResponse(await FindAsync(id, ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        _db.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<ChangeRequest> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(c => c.Id == id, ct) ?? throw new KeyNotFoundException("Change request not found");

    private async Task ValidateAsync(long projectId, long requestedById, CancellationToken ct)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId, ct)) throw new ArgumentException("Project not found");
        if (!await _db.Users.AnyAsync(u => u.Id == requestedById, ct)) throw new ArgumentException("Requester not found");
    }
}
