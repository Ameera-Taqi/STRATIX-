using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>
/// A tenant. Every user, department, project, and downstream record belongs to exactly one
/// organization. This is the root of the multi-tenant model (shared database, shared schema).
/// </summary>
public class Organization : IHasCreatedAt, IHasUpdatedAt
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public OrganizationStatus Status { get; set; } = OrganizationStatus.ACTIVE;

    /// <summary>
    /// Denormalized mirror of <see cref="Subscription.PlanCode"/> for convenient reads.
    /// <b>Source of truth for the active plan is <see cref="Subscription"/>.</b>
    /// Quotas come from <see cref="PlanTier"/> via that plan code.
    /// </summary>
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.FREE;

    /// <summary>Relative storage key for the tenant logo file (e.g. "org-12.png"), or null for default branding.</summary>
    public string? LogoFileName { get; set; }

    /// <summary>Optional industry label from onboarding / settings.</summary>
    public string? Industry { get; set; }

    /// <summary>IANA timezone id (e.g. Asia/Riyadh).</summary>
    public string? Timezone { get; set; }

    /// <summary>Preferred UI language code: en | ar.</summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>When set, ORG_ADMIN has finished or skipped the guided onboarding wizard.</summary>
    public DateTimeOffset? OnboardingCompletedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
