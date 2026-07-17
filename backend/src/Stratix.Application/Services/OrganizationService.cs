using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Organizations;
using Stratix.Application.Interfaces;

namespace Stratix.Application.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public OrganizationService(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    // The current caller's organization. User/project counts are tenant-filtered automatically.
    public async Task<OrganizationResponse?> GetCurrentAsync(CancellationToken ct = default)
    {
        if (_tenant.OrganizationId is not long orgId) return null;
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct);
        if (org == null) return null;

        var users = await _db.Users.CountAsync(ct);
        var projects = await _db.Projects.CountAsync(ct);
        return new OrganizationResponse(org.Id, org.Name, org.Slug, org.Status.ToString(),
            org.SubscriptionPlan.ToString(), org.CreatedAt, users, projects);
    }

    // Platform-wide list — only reachable by SUPER_ADMIN (see controller). The tenant filter
    // is bypassed for super-admins, so per-org counts are computed across every tenant.
    public async Task<IReadOnlyList<OrganizationResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var orgs = await _db.Organizations.OrderBy(o => o.Id).ToListAsync(ct);
        var userCounts = await _db.Users.GroupBy(u => u.OrganizationId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var projectCounts = await _db.Projects.GroupBy(p => p.OrganizationId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);

        return orgs.Select(o => new OrganizationResponse(
            o.Id, o.Name, o.Slug, o.Status.ToString(), o.SubscriptionPlan.ToString(), o.CreatedAt,
            userCounts.GetValueOrDefault(o.Id), projectCounts.GetValueOrDefault(o.Id))).ToList();
    }

    // The caller's own subscription (tenant-filtered automatically).
    public async Task<SubscriptionResponse?> GetCurrentSubscriptionAsync(CancellationToken ct = default)
    {
        var s = await _db.Subscriptions.FirstOrDefaultAsync(ct);
        return s == null ? null
            : new SubscriptionResponse(s.OrganizationId, s.PlanCode, s.Status.ToString(), s.StartedAt, s.TrialEndsAt, s.EndsAt);
    }

    public async Task<IReadOnlyList<PlanResponse>> GetPlansAsync(CancellationToken ct = default)
    {
        return await _db.Plans.OrderBy(p => p.Price)
            .Select(p => new PlanResponse(p.Id, p.Name, p.MaxUsers, p.MaxProjects, p.AiEnabled, p.StorageLimitMb, p.Price))
            .ToListAsync(ct);
    }
}
