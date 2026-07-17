using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>
/// A tenant. Every user, department, project, and downstream record belongs to exactly one
/// organization. This is the root of the multi-tenant model (shared database, shared schema).
/// </summary>
public class Organization
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public OrganizationStatus Status { get; set; } = OrganizationStatus.ACTIVE;
    public SubscriptionPlan SubscriptionPlan { get; set; } = SubscriptionPlan.FREE;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
