using Microsoft.EntityFrameworkCore;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

/// <summary>
/// Enforces org/subscription access for login and refresh.
/// Applies trial/subscription end dates by flipping status when due.
/// </summary>
public class OrganizationAccessService
{
    private readonly IApplicationDbContext _db;

    public OrganizationAccessService(IApplicationDbContext db) => _db = db;

    public async Task EnsureCanAuthenticateAsync(User user, CancellationToken ct = default)
    {
        // Platform owners stay attached to a home tenant for FKs but must always sign in.
        if (user.Role == UserRole.SUPER_ADMIN) return;

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == user.OrganizationId, ct);
        if (org == null) return;

        if (org.Status is OrganizationStatus.SUSPENDED or OrganizationStatus.CANCELLED)
            throw new UnauthorizedAccessException(
                $"Your organization's account is {org.Status.ToString().ToLowerInvariant()}. Contact your administrator or support.");

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrganizationId == user.OrganizationId, ct);
        if (subscription == null) return;

        await ApplyExpiryAsync(org, subscription, ct);

        if (subscription.Status == SubscriptionStatus.CANCELLED)
            throw new UnauthorizedAccessException(
                "Your organization's subscription has ended or been cancelled. Contact your administrator or support.");

        if (subscription.Status == SubscriptionStatus.PAST_DUE)
            throw new UnauthorizedAccessException(
                "Your organization's subscription is past due. Contact your administrator or support.");
    }

    /// <summary>
    /// Persists expiry when trial/paid period has ended. Idempotent.
    /// </summary>
    public async Task ApplyExpiryAsync(Organization org, Subscription subscription, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var changed = false;

        if (subscription.Status == SubscriptionStatus.TRIALING
            && subscription.TrialEndsAt is { } trialEnd
            && trialEnd <= now)
        {
            subscription.Status = SubscriptionStatus.CANCELLED;
            subscription.EndsAt ??= now;
            changed = true;
        }
        else if (subscription.Status is SubscriptionStatus.ACTIVE or SubscriptionStatus.PAST_DUE
                 && subscription.EndsAt is { } endsAt
                 && endsAt <= now)
        {
            subscription.Status = SubscriptionStatus.CANCELLED;
            changed = true;
        }

        if (!changed) return;

        if (org.Status == OrganizationStatus.ACTIVE)
            org.Status = OrganizationStatus.SUSPENDED;
        org.UpdatedAt = now;
        await _db.SaveChangesAsync(ct);
    }
}
