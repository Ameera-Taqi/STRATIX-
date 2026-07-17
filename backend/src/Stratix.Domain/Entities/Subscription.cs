using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>
/// The billing subscription for one organization (one-to-one). Created with a 14-day trial
/// when the organization registers. Tenant-scoped like all other org data.
/// </summary>
public class Subscription : ITenantScoped
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
