using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stratix.Application.Interfaces;
using Stratix.Infrastructure.Persistence;

namespace Stratix.Infrastructure.Background;

/// <summary>
/// Removes report files on disk that are not referenced by any report row
/// (including soft-deleted rows that failed mid-delete, or create failures after SaveAsync).
/// </summary>
public sealed class ReportOrphanCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReportOrphanCleanupService> _logger;

    public ReportOrphanCleanupService(IServiceScopeFactory scopeFactory, ILogger<ReportOrphanCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Delay first run so startup seeding/API warm-up finishes.
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Report orphan cleanup failed.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task CleanupOnceAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<IReportFileStorage>();
        var db = scope.ServiceProvider.GetRequiredService<StratixDbContext>();

        var onDisk = storage.ListStoredFiles();
        if (onDisk.Count == 0) return;

        var referenced = await db.ReportSet
            .IgnoreQueryFilters()
            .Select(r => r.StorageKey)
            .ToListAsync(ct);

        var known = new HashSet<string>(referenced, StringComparer.OrdinalIgnoreCase);
        var removed = 0;
        foreach (var (orgId, key) in onDisk)
        {
            if (known.Contains(key)) continue;
            storage.Delete(orgId, key);
            removed++;
        }

        if (removed > 0)
            _logger.LogInformation("Removed {Count} orphaned report file(s).", removed);
    }
}
