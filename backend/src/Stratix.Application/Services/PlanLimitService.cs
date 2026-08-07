using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;

namespace Stratix.Application.Services;

/// <summary>
/// Enforces the current organization's subscription quotas (max users / max projects)
/// before new records are created. Skipped for platform (super-admin) / system callers.
/// </summary>
public interface IPlanLimitService
{
    Task EnsureCanAddUserAsync(CancellationToken ct = default);
    Task EnsureCanAddProjectAsync(CancellationToken ct = default);
    Task EnsureAiEnabledAsync(CancellationToken ct = default);
    Task EnsureStorageAvailableAsync(long additionalBytes, CancellationToken ct = default);
}

public class PlanLimitService : IPlanLimitService
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;

    public PlanLimitService(IApplicationDbContext db, ITenantContext tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public async Task EnsureCanAddUserAsync(CancellationToken ct = default)
    {
        var plan = await ResolvePlanAsync(ct);
        if (plan is null) return;
        var count = await _db.Users.CountAsync(ct);
        if (count >= plan.MaxUsers)
            throw new PlanLimitExceededException($"Your plan ({plan.Name}) allows up to {plan.MaxUsers} users. Upgrade to add more.");
    }

    public async Task EnsureCanAddProjectAsync(CancellationToken ct = default)
    {
        var plan = await ResolvePlanAsync(ct);
        if (plan is null) return;
        var count = await _db.Projects.CountAsync(ct);
        if (count >= plan.MaxProjects)
            throw new PlanLimitExceededException($"Your plan ({plan.Name}) allows up to {plan.MaxProjects} projects. Upgrade to add more.");
    }

    public async Task EnsureAiEnabledAsync(CancellationToken ct = default)
    {
        var plan = await ResolvePlanAsync(ct);
        if (plan is null) return;
        if (!plan.AiEnabled)
            throw new PlanLimitExceededException($"Your plan ({plan.Name}) does not include AI features. Upgrade to enable them.");
    }

    public async Task EnsureStorageAvailableAsync(long additionalBytes, CancellationToken ct = default)
    {
        var plan = await ResolvePlanAsync(ct);
        if (plan is null) return;

        var usedFiles = await _db.ProjectFiles.SumAsync(f => (long?)f.SizeBytes, ct) ?? 0;
        var usedReports = await _db.Reports.SumAsync(r => (long?)r.SizeBytes, ct) ?? 0;
        var usedBytes = usedFiles + usedReports;
        var limitBytes = plan.StorageLimitMb * 1024 * 1024;
        if (usedBytes + additionalBytes > limitBytes)
            throw new PlanLimitExceededException($"Your plan ({plan.Name}) allows up to {plan.StorageLimitMb} MB of storage. Upgrade to add more.");
    }

    // Resolves the tier (and its quotas) for the caller's organization. Returns null when
    // there is no tenant scope (super-admin/system) or no matching tier — i.e. no limit.
    // Source of truth: subscriptions.plan_code → subscription_plans (PlanTier) catalog.
    private async Task<PlanTier?> ResolvePlanAsync(CancellationToken ct)
    {
        if (!_tenant.HasTenantScope) return null;

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(ct);
        var tierName = PlanCodes.ToTierName(subscription?.PlanCode);
        return await _db.Plans.FirstOrDefaultAsync(p => p.Name == tierName, ct);
    }
}

/// <summary>Raised when an action would exceed the organization's plan quota (maps to HTTP 402/409).</summary>
public class PlanLimitExceededException : Exception
{
    public PlanLimitExceededException(string message) : base(message) { }
}
