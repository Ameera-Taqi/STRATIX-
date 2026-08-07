namespace Stratix.Domain.Entities;

/// <summary>
/// Catalog of subscription tiers (Starter / Professional / Enterprise).
/// <b>Source of truth</b> for quotas and capabilities (max users/projects, AI, storage).
/// Not tenant-scoped — shared across the platform.
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
