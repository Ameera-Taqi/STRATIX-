namespace Stratix.Domain.Entities;

/// <summary>
/// A subscription tier available to organizations (Starter / Professional / Enterprise).
/// Defines the quotas and capabilities granted to a tenant on that plan.
/// </summary>
public class PlanTier
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxProjects { get; set; }
    public bool AiEnabled { get; set; }
    public long StorageLimitMb { get; set; }
    public decimal Price { get; set; }
}
