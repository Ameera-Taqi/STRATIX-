using Microsoft.Extensions.Configuration;
using Stratix.Application.Common;
using Stratix.Application.DTOs;
using Stratix.Application.Interfaces;

namespace Stratix.Application.Services;

public class HealthService : IHealthService
{
    private const long DefaultMinFreeBytes = 50L * 1024 * 1024;

    private readonly IApplicationDbContext _db;
    private readonly ISchemaHealthProbe _schema;
    private readonly ITenantFileStorage _storage;
    private readonly long _minFreeBytes;

    public HealthService(
        IApplicationDbContext db,
        ISchemaHealthProbe schema,
        ITenantFileStorage storage,
        IConfiguration configuration)
    {
        _db = db;
        _schema = schema;
        _storage = storage;
        _minFreeBytes = long.TryParse(configuration["Stratix:Storage:MinFreeBytes"], out var bytes) && bytes > 0
            ? bytes
            : DefaultMinFreeBytes;
    }

    public Task<LivenessResponse> GetLivenessAsync(CancellationToken ct = default) =>
        Task.FromResult(new LivenessResponse("UP", "stratix-api"));

    public async Task<ReadinessResponse> GetReadinessAsync(CancellationToken ct = default)
    {
        var checks = new List<ReadinessCheck>
        {
            await CheckDatabaseAsync(ct),
            CheckStorage(),
            await CheckMigrationsAsync(ct)
        };

        var status = checks.All(c => c.Status == "UP") ? "UP" : "DOWN";
        return new ReadinessResponse(status, "stratix-api", checks);
    }

    public async Task<HealthResponse> GetHealthAsync(CancellationToken ct = default)
    {
        var ready = await GetReadinessAsync(ct);
        var db = ready.Checks.FirstOrDefault(c => c.Name == "database");
        return new HealthResponse(
            ready.Application,
            ready.Status,
            db?.Status ?? "DOWN",
            _db.GetDatabaseProductName(),
            db?.Status == "UP" ? null : db?.Detail);
    }

    private async Task<ReadinessCheck> CheckDatabaseAsync(CancellationToken ct)
    {
        try
        {
            var ok = await _schema.CanQueryAsync(ct);
            return ok
                ? new ReadinessCheck("database", "UP", _db.GetDatabaseProductName())
                : new ReadinessCheck("database", "DOWN", "Cannot query SQL Server");
        }
        catch (Exception ex)
        {
            return new ReadinessCheck("database", "DOWN", ex.Message);
        }
    }

    private ReadinessCheck CheckStorage()
    {
        try
        {
            var root = _storage.Root;
            Directory.CreateDirectory(root);

            var probe = Path.Combine(root, $".health-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);

            long? free = null;
            try
            {
                var full = Path.GetFullPath(root);
                var driveRoot = Path.GetPathRoot(full);
                if (!string.IsNullOrWhiteSpace(driveRoot))
                {
                    var drive = new DriveInfo(driveRoot);
                    if (drive.IsReady)
                        free = drive.AvailableFreeSpace;
                }
            }
            catch
            {
                // Free-space APIs can fail on some mounts; writability is enough to stay UP.
            }

            if (free is long available && available < _minFreeBytes)
            {
                return new ReadinessCheck(
                    "storage",
                    "DOWN",
                    $"Low disk space: {available} bytes free (min {_minFreeBytes}) under {root}");
            }

            var detail = free is long f
                ? $"writable; {f} bytes free under {root}"
                : $"writable under {root}";
            return new ReadinessCheck("storage", "UP", detail);
        }
        catch (Exception ex)
        {
            return new ReadinessCheck("storage", "DOWN", ex.Message);
        }
    }

    private async Task<ReadinessCheck> CheckMigrationsAsync(CancellationToken ct)
    {
        try
        {
            if (!_schema.IsRelational)
                return new ReadinessCheck("migrations", "UP", "skipped (non-relational provider)");

            var required = RequiredSchemaMigrations.All;
            var applied = await _schema.GetAppliedMigrationIdsAsync(ct);
            var appliedSet = applied.ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (appliedSet.Count > 0)
            {
                var missing = required.Where(r => !appliedSet.Contains(r)).ToList();
                return missing.Count == 0
                    ? new ReadinessCheck("migrations", "UP", $"{appliedSet.Count} recorded")
                    : new ReadinessCheck("migrations", "DOWN", "Missing: " + string.Join(", ", missing));
            }

            // Tracking table empty/missing — fall back to structural markers for milestones.
            var structuralMissing = await _schema.GetMissingStructuralMigrationsAsync(required, ct);
            return structuralMissing.Count == 0
                ? new ReadinessCheck("migrations", "UP", "structural markers ok (schema_migrations empty)")
                : new ReadinessCheck("migrations", "DOWN", "Missing markers: " + string.Join(", ", structuralMissing));
        }
        catch (Exception ex)
        {
            return new ReadinessCheck("migrations", "DOWN", ex.Message);
        }
    }
}
