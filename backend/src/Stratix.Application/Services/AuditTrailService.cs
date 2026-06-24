using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Audit;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class AuditTrailService : IAuditTrailService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AuditTrailService(IApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<AuditLogPageResponse> SearchAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var size = Math.Clamp(query.Size, 1, 100);
        var page = Math.Max(query.Page, 0);
        var q = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EntityType) && Enum.TryParse<AuditEntityType>(query.EntityType, true, out var entityType))
            q = q.Where(a => a.EntityType == entityType);
        if (query.EntityId.HasValue) q = q.Where(a => a.EntityId == query.EntityId);
        if (query.UserId.HasValue) q = q.Where(a => a.UserId == query.UserId);
        if (!string.IsNullOrWhiteSpace(query.Action) && Enum.TryParse<AuditAction>(query.Action, true, out var action))
            q = q.Where(a => a.Action == action);
        if (query.ProjectId.HasValue) q = q.Where(a => a.ProjectId == query.ProjectId);
        if (query.StartDate.HasValue) q = q.Where(a => a.CreatedAt >= query.StartDate);
        if (query.EndDate.HasValue) q = q.Where(a => a.CreatedAt <= query.EndDate);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(a => (a.EntityName != null && a.EntityName.Contains(term)) ||
                             (a.Description != null && a.Description.Contains(term)) ||
                             (a.UserName != null && a.UserName.Contains(term)));
        }

        var total = await q.CountAsync(ct);
        var content = await q.OrderByDescending(a => a.CreatedAt)
            .Skip(page * size).Take(size)
            .Select(a => EntityMappers.ToResponse(a))
            .ToListAsync(ct);

        var totalPages = size > 0 ? (int)Math.Ceiling(total / (double)size) : 0;
        return new AuditLogPageResponse(content, total, totalPages, page, size);
    }

    public Task RecordCreateAsync(AuditEntityType entityType, long entityId, string entityName, string? snapshot, string description, long? projectId, string? projectName, CancellationToken ct = default) =>
        RecordAsync(AuditAction.CREATE, entityType, entityId, entityName, null, snapshot, description, projectId, projectName, ct);

    public Task RecordDeleteAsync(AuditEntityType entityType, long entityId, string entityName, string? snapshot, string description, long? projectId, string? projectName, CancellationToken ct = default) =>
        RecordAsync(AuditAction.DELETE, entityType, entityId, entityName, snapshot, null, description, projectId, projectName, ct);

    private async Task RecordAsync(AuditAction action, AuditEntityType entityType, long entityId, string entityName, string? oldValues, string? newValues, string description, long? projectId, string? projectName, CancellationToken ct)
    {
        _db.Add(new AuditLog
        {
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            OldValues = oldValues,
            NewValues = newValues,
            Description = description,
            ProjectId = projectId,
            ProjectName = projectName,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
