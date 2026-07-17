namespace Stratix.Domain.Entities;

/// <summary>
/// Tenant-defined role. Access follows <see cref="BaseRole"/> (a system UserRole code)
/// so existing JWT / [Authorize(Roles=…)] checks keep working.
/// </summary>
public class OrganizationRole : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    /// <summary>System role code that this custom role inherits (e.g. EMPLOYEE).</summary>
    public string BaseRole { get; set; } = "EMPLOYEE";
    public bool IsSystem { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
