using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Organizations;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class OrganizationService : IOrganizationService
{
    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IPasswordHasher _passwordHasher;

    public OrganizationService(IApplicationDbContext db, ITenantContext tenant, IPasswordHasher passwordHasher)
    {
        _db = db;
        _tenant = tenant;
        _passwordHasher = passwordHasher;
    }

    // The current caller's organization. User/project counts are tenant-filtered automatically.
    public async Task<OrganizationResponse?> GetCurrentAsync(CancellationToken ct = default)
    {
        if (_tenant.OrganizationId is not long orgId) return null;
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct);
        if (org == null) return null;

        var users = await _db.Users.CountAsync(ct);
        var projects = await _db.Projects.CountAsync(ct);
        var plan = await ResolvePlanDisplayAsync(org, ct);
        return new OrganizationResponse(org.Id, org.Name, org.Slug, org.Status.ToString(),
            plan, org.CreatedAt, users, projects);
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
        var planByOrg = await _db.Subscriptions
            .Select(s => new { s.OrganizationId, s.PlanCode })
            .ToDictionaryAsync(x => x.OrganizationId, x => x.PlanCode, ct);

        return orgs.Select(o =>
        {
            var planCode = planByOrg.GetValueOrDefault(o.Id);
            var plan = planCode != null
                ? PlanCodes.ToOrganizationPlan(planCode).ToString()
                : o.SubscriptionPlan.ToString();
            return new OrganizationResponse(o.Id, o.Name, o.Slug, o.Status.ToString(), plan, o.CreatedAt,
                userCounts.GetValueOrDefault(o.Id), projectCounts.GetValueOrDefault(o.Id));
        }).ToList();
    }

    public async Task<OrganizationResponse> CreateAsync(CreateOrganizationRequest request, CancellationToken ct = default)
    {
        var name = request.OrganizationName?.Trim() ?? "";
        var adminName = request.AdminName?.Trim() ?? "";
        var email = request.AdminEmail?.Trim().ToLowerInvariant() ?? "";

        if (name.Length == 0 || adminName.Length == 0 || email.Length == 0 || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Organization name, admin name, email and password are required.");
        if (request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Email already in use.");

        var plan = SubscriptionPlan.FREE;
        if (!string.IsNullOrWhiteSpace(request.SubscriptionPlan)
            && !Enum.TryParse(request.SubscriptionPlan.Trim(), ignoreCase: true, out plan))
            throw new ArgumentException($"Invalid subscription plan: {request.SubscriptionPlan}");

        var now = DateTimeOffset.UtcNow;

        await using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var org = new Organization
            {
                Name = name,
                Slug = await GenerateUniqueSlugAsync(request.Slug, name, ct),
                Status = OrganizationStatus.ACTIVE,
                SubscriptionPlan = plan,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Add(org);
            await _db.SaveChangesAsync(ct);

            _db.Add(new User
            {
                OrganizationId = org.Id,
                Name = adminName,
                Email = email,
                Password = _passwordHasher.Hash(request.Password),
                Role = UserRole.ORG_ADMIN,
                JobTitle = "Organization Administrator",
                Status = UserStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now
            });

            _db.Add(new Subscription
            {
                OrganizationId = org.Id,
                PlanCode = PlanCodes.FromOrganizationPlan(plan),
                Status = SubscriptionStatus.ACTIVE,
                StartedAt = now,
                CreatedAt = now
            });
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return new OrganizationResponse(org.Id, org.Name, org.Slug, org.Status.ToString(),
                plan.ToString(), org.CreatedAt, 1, 0);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<OrganizationResponse?> UpdateAsync(long id, UpdateOrganizationRequest request, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (org == null) return null;

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<OrganizationStatus>(request.Status.Trim(), ignoreCase: true, out var status))
                throw new ArgumentException($"Invalid organization status: {request.Status}");
            org.Status = status;
        }

        if (!string.IsNullOrWhiteSpace(request.SubscriptionPlan))
        {
            if (!Enum.TryParse<SubscriptionPlan>(request.SubscriptionPlan.Trim(), ignoreCase: true, out var plan))
                throw new ArgumentException($"Invalid subscription plan: {request.SubscriptionPlan}");

            // Subscription is the source of truth; org.SubscriptionPlan is a denormalized mirror.
            var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrganizationId == id, ct);
            if (subscription != null)
            {
                subscription.PlanCode = PlanCodes.FromOrganizationPlan(plan);
                if (subscription.Status is SubscriptionStatus.CANCELLED or SubscriptionStatus.PAST_DUE or SubscriptionStatus.TRIALING)
                    subscription.Status = SubscriptionStatus.ACTIVE;
            }
            else
            {
                _db.Add(new Subscription
                {
                    OrganizationId = id,
                    PlanCode = PlanCodes.FromOrganizationPlan(plan),
                    Status = SubscriptionStatus.ACTIVE,
                    StartedAt = DateTimeOffset.UtcNow,
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }

            org.SubscriptionPlan = plan;
        }

        org.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var users = await _db.Users.CountAsync(u => u.OrganizationId == id, ct);
        var projects = await _db.Projects.CountAsync(p => p.OrganizationId == id, ct);
        return new OrganizationResponse(org.Id, org.Name, org.Slug, org.Status.ToString(),
            await ResolvePlanDisplayAsync(org, ct), org.CreatedAt, users, projects);
    }

    // Soft-delete: mark the organization CANCELLED and cancel its subscription rather than
    // hard-deleting the tenant's data, so its history (projects, tasks, audit trail) survives.
    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == id, ct)
            ?? throw new KeyNotFoundException("Organization not found");

        org.Status = OrganizationStatus.CANCELLED;
        org.UpdatedAt = DateTimeOffset.UtcNow;

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrganizationId == id, ct);
        if (subscription != null)
        {
            subscription.Status = SubscriptionStatus.CANCELLED;
            subscription.EndsAt = DateTimeOffset.UtcNow;
        }

        var now = DateTimeOffset.UtcNow;
        var tokens = await _db.RefreshTokens
            .Where(t => t.OrganizationId == id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in tokens)
            token.RevokedAt = now;

        await _db.SaveChangesAsync(ct);
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

    private async Task<string> ResolvePlanDisplayAsync(Organization org, CancellationToken ct)
    {
        var planCode = await _db.Subscriptions
            .Where(s => s.OrganizationId == org.Id)
            .Select(s => s.PlanCode)
            .FirstOrDefaultAsync(ct);
        return planCode != null
            ? PlanCodes.ToOrganizationPlan(planCode).ToString()
            : org.SubscriptionPlan.ToString();
    }

    private async Task<string> GenerateUniqueSlugAsync(string? requestedSlug, string name, CancellationToken ct)
    {
        var source = !string.IsNullOrWhiteSpace(requestedSlug) ? requestedSlug! : name;
        var baseSlug = new string(source.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-');
        while (baseSlug.Contains("--")) baseSlug = baseSlug.Replace("--", "-");
        if (baseSlug.Length == 0) baseSlug = "org";

        var slug = baseSlug;
        var suffix = 1;
        while (await _db.Organizations.AnyAsync(o => o.Slug == slug, ct))
            slug = $"{baseSlug}-{++suffix}";
        return slug;
    }
}
