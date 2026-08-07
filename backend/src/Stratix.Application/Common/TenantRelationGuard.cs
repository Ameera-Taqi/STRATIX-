using Microsoft.EntityFrameworkCore;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;

namespace Stratix.Application.Common;

/// <summary>
/// Ensures FK targets belong to the same tenant as the row being written.
/// Required for SUPER_ADMIN / unscoped writers where EF query filters are off.
/// </summary>
public class TenantRelationGuard
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public TenantRelationGuard(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>Organization id for the write: entity value, else JWT tenant claim.</summary>
    public long ResolveOrganizationId(long entityOrganizationId)
    {
        if (entityOrganizationId > 0) return entityOrganizationId;
        if (_tenant.OrganizationId is long oid && oid > 0) return oid;
        throw new InvalidOperationException(
            "OrganizationId is required to validate cross-tenant relationships.");
    }

    public async Task EnsureDepartmentAsync(long? departmentId, long organizationId, CancellationToken ct = default)
    {
        if (departmentId is null) return;
        if (!await _db.Departments.AnyAsync(d => d.Id == departmentId && d.OrganizationId == organizationId, ct))
            throw new ArgumentException("Department does not belong to this organization.");
    }

    public async Task EnsureUserAsync(long? userId, long organizationId, CancellationToken ct = default)
    {
        if (userId is null) return;
        if (!await _db.Users.AnyAsync(u => u.Id == userId && u.OrganizationId == organizationId, ct))
            throw new ArgumentException("User does not belong to this organization.");
    }

    public async Task EnsureUserRequiredAsync(long userId, long organizationId, CancellationToken ct = default)
    {
        if (!await _db.Users.AnyAsync(u => u.Id == userId && u.OrganizationId == organizationId, ct))
            throw new ArgumentException("User does not belong to this organization.");
    }

    public async Task EnsureProjectAsync(long projectId, long organizationId, CancellationToken ct = default)
    {
        if (!await _db.Projects.AnyAsync(p => p.Id == projectId && p.OrganizationId == organizationId, ct))
            throw new ArgumentException("Project does not belong to this organization.");
    }

    public async Task EnsureStageInProjectAsync(long? stageId, long projectId, long organizationId, CancellationToken ct = default)
    {
        if (stageId is null) return;
        var stage = await _db.ProjectStages
            .FirstOrDefaultAsync(s => s.Id == stageId && s.OrganizationId == organizationId, ct)
            ?? throw new ArgumentException("Feature does not belong to this organization.");
        if (stage.ProjectId != projectId)
            throw new ArgumentException("Feature does not belong to project.");
    }

    public async Task EnsureOrganizationRoleAsync(long? roleId, long organizationId, CancellationToken ct = default)
    {
        if (roleId is null) return;
        if (!await _db.OrganizationRoles.AnyAsync(r => r.Id == roleId && r.OrganizationId == organizationId, ct))
            throw new ArgumentException("Organization role does not belong to this organization.");
    }
}
