using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Stratix.Application.Interfaces;
using Stratix.Infrastructure.Persistence;

namespace Stratix.Infrastructure.Health;

public sealed class SchemaHealthProbe : ISchemaHealthProbe
{
    private readonly StratixDbContext _db;
    private readonly string? _connectionString;

    public SchemaHealthProbe(StratixDbContext db, IConfiguration configuration)
    {
        _db = db;
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? configuration["DB_CONNECTION"];
    }

    public bool IsRelational => _db.Database.IsRelational();

    public async Task<bool> CanQueryAsync(CancellationToken ct = default)
    {
        if (!IsRelational)
            return await _db.Database.CanConnectAsync(ct);

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT 1";
        cmd.CommandTimeout = 5;
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is int or long || (result is not null && result.ToString() == "1");
    }

    public async Task<IReadOnlyCollection<string>> GetAppliedMigrationIdsAsync(CancellationToken ct = default)
    {
        if (!IsRelational || string.IsNullOrWhiteSpace(_connectionString))
            return Array.Empty<string>();

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        await using (var existsCmd = conn.CreateCommand())
        {
            existsCmd.CommandText = "SELECT OBJECT_ID(N'dbo.schema_migrations', N'U')";
            existsCmd.CommandTimeout = 5;
            var id = await existsCmd.ExecuteScalarAsync(ct);
            if (id is null || id is DBNull)
                return Array.Empty<string>();
        }

        var list = new List<string>();
        await using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT migration_id FROM dbo.schema_migrations";
            cmd.CommandTimeout = 5;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
                list.Add(reader.GetString(0));
        }

        return list;
    }

    public async Task<IReadOnlyCollection<string>> GetMissingStructuralMigrationsAsync(
        IEnumerable<string> required,
        CancellationToken ct = default)
    {
        if (!IsRelational || string.IsNullOrWhiteSpace(_connectionString))
            return Array.Empty<string>();

        var requiredSet = required.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missing = new List<string>();

        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(ct);

        foreach (var id in requiredSet)
        {
            if (!StructuralMarkers.TryGetValue(id, out var sql))
                continue; // no marker → rely on schema_migrations only

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandTimeout = 5;
            var ok = await cmd.ExecuteScalarAsync(ct);
            var present = ok is not null and not DBNull && Convert.ToInt32(ok) == 1;
            if (!present)
                missing.Add(id);
        }

        return missing;
    }

    /// <summary>Lightweight markers for milestone migrations (used when tracking table is empty).</summary>
    private static readonly Dictionary<string, string> StructuralMarkers = new(StringComparer.OrdinalIgnoreCase)
    {
        ["016_multitenancy.sqlserver.sql"] =
            "SELECT CASE WHEN COL_LENGTH(N'dbo.users', N'organization_id') IS NOT NULL THEN 1 ELSE 0 END",
        ["028_soft_delete.sql"] =
            "SELECT CASE WHEN COL_LENGTH(N'dbo.users', N'is_deleted') IS NOT NULL THEN 1 ELSE 0 END",
        ["030_reports_module.sql"] =
            "SELECT CASE WHEN OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL THEN 1 ELSE 0 END",
        ["033_global_unique_email.sql"] =
            "SELECT CASE WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_users_email_active' AND object_id = OBJECT_ID(N'dbo.users')) THEN 1 ELSE 0 END",
        ["034_refresh_token_reuse_detection.sql"] =
            "SELECT CASE WHEN COL_LENGTH(N'dbo.refresh_tokens', N'token_family_id') IS NOT NULL THEN 1 ELSE 0 END",
        ["035_schema_migrations.sql"] =
            "SELECT CASE WHEN OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL THEN 1 ELSE 0 END",
        ["046_audit_logs.sql"] =
            "SELECT CASE WHEN OBJECT_ID(N'dbo.audit_logs', N'U') IS NOT NULL THEN 1 ELSE 0 END",
    };
}
