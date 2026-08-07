using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditTrailService(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _currentUser = currentUser;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<AuditLogPageResponse> SearchAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = PageQuery.Normalize(query.Page, query.Size);
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
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(a => EntityMappers.ToResponse(a))
            .ToListAsync(ct);

        var totalPages = pageSize > 0 ? (int)Math.Ceiling(total / (double)pageSize) : 0;
        return new AuditLogPageResponse(content, total, totalPages, page, pageSize);
    }

    public Task RecordCreateAsync(AuditEntityType entityType, long entityId, string entityName, string? snapshot, string description, long? projectId, string? projectName, CancellationToken ct = default) =>
        RecordAsync(AuditAction.CREATE, entityType, entityId, entityName, null, snapshot, description, projectId, projectName, ct);

    public Task RecordUpdateAsync(AuditEntityType entityType, long entityId, string entityName, string? oldValues, string? newValues, string description, long? projectId, string? projectName, CancellationToken ct = default) =>
        RecordAsync(AuditAction.UPDATE, entityType, entityId, entityName, oldValues, newValues, description, projectId, projectName, ct);

    public Task RecordDeleteAsync(AuditEntityType entityType, long entityId, string entityName, string? snapshot, string description, long? projectId, string? projectName, CancellationToken ct = default) =>
        RecordAsync(AuditAction.DELETE, entityType, entityId, entityName, snapshot, null, description, projectId, projectName, ct);

    private async Task RecordAsync(AuditAction action, AuditEntityType entityType, long entityId, string entityName, string? oldValues, string? newValues, string description, long? projectId, string? projectName, CancellationToken ct)
    {
        var http = _httpContextAccessor.HttpContext;
        _db.Add(new AuditLog
        {
            OrganizationId = _currentUser.OrganizationId ?? 0,
            UserId = _currentUser.UserId,
            UserName = _currentUser.UserName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            OldValues = oldValues,
            NewValues = newValues,
            Description = description,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = Truncate(http?.Request.Headers.UserAgent.ToString(), 500),
            ProjectId = projectId,
            ProjectName = projectName,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrEmpty(value) ? value : value.Length <= max ? value : value[..max];
}
