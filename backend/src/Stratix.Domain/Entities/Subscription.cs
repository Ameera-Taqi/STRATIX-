using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>
/// The billing subscription for one organization (one-to-one).
/// <b>Source of truth</b> for the tenant's current plan code and billing status.
/// Quotas/capabilities are resolved from <see cref="PlanTier"/> using <see cref="PlanCode"/>.
/// <see cref="Organization.SubscriptionPlan"/> is kept as a denormalized mirror only.
/// </summary>
public class Subscription : ITenantScoped, IHasCreatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string PlanCode { get; set; } = "TRIAL";
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.TRIALING;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? TrialEndsAt { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
