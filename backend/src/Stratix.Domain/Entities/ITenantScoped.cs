namespace Stratix.Domain.Entities;

/// <summary>
/// Marks an entity as belonging to a single tenant (organization). Every tenant-scoped
/// table carries an <see cref="OrganizationId"/>. The persistence layer enforces isolation
/// automatically via EF global query filters and stamps this value on insert, so no query
/// can ever leak data across organizations.
/// </summary>
public interface ITenantScoped
{
    long OrganizationId { get; set; }
}
