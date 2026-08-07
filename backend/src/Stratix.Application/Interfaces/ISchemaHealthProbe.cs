namespace Stratix.Application.Interfaces;

/// <summary>Database probes used by readiness checks (relational SQL Server).</summary>
public interface ISchemaHealthProbe
{
    bool IsRelational { get; }

    Task<bool> CanQueryAsync(CancellationToken ct = default);

    /// <summary>Ids recorded in <c>dbo.schema_migrations</c> (empty if table missing).</summary>
    Task<IReadOnlyCollection<string>> GetAppliedMigrationIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Structural fallback when the migrations table is empty/missing:
    /// returns required ids that lack a confirming schema marker.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetMissingStructuralMigrationsAsync(
        IEnumerable<string> required,
        CancellationToken ct = default);
}
