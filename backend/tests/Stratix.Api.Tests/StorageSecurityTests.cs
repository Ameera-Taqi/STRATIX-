using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Stratix.Application.Interfaces;
using Stratix.Infrastructure.Background;
using Stratix.Infrastructure.Persistence;

namespace Stratix.Api.Tests;

public class StorageSecurityTests : IClassFixture<StratixApiFactory>
{
    private readonly StratixApiFactory _factory;

    public StorageSecurityTests(StratixApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Path_traversal_and_cross_org_keys_are_rejected()
    {
        using var scope = _factory.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<ITenantFileStorage>();

        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("ok"));
        var key = await storage.SaveAsync(1, StorageCategories.Reports, "safe.pdf", content);

        Assert.StartsWith("org-1/", key, StringComparison.OrdinalIgnoreCase);

        var traversal = await storage.OpenAsync(1, StorageCategories.Reports, "org-1/../../etc/passwd", "application/pdf");
        Assert.Null(traversal);

        var wrongOrg = await storage.OpenAsync(1, StorageCategories.Reports, "org-2/safe.pdf", "application/pdf");
        Assert.Null(wrongOrg);

        var escaped = await storage.OpenAsync(1, StorageCategories.Reports, "../org-1/safe.pdf", "application/pdf");
        Assert.Null(escaped);

        var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
        {
            await using var evil = new MemoryStream([1]);
            await storage.SaveAsync(1, "../evil", "x.bin", evil);
        });
        Assert.Contains("Invalid storage category", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Report_orphan_cleanup_removes_unreferenced_files()
    {
        using var scope = _factory.Services.CreateScope();
        var reports = scope.ServiceProvider.GetRequiredService<IReportFileStorage>();
        var db = scope.ServiceProvider.GetRequiredService<StratixDbContext>();
        var scopes = scope.ServiceProvider.GetRequiredService<IServiceScopeFactory>();

        var orgId = await db.OrganizationSet.Select(o => o.Id).FirstAsync();
        await using var bytes = new MemoryStream(Encoding.UTF8.GetBytes("%PDF-1.4 orphan"));
        var orphanKey = await reports.SaveAsync(orgId, $"orphan-{Guid.NewGuid():N}.pdf", bytes);

        Assert.Contains(reports.ListStoredFiles(), f => f.StorageKey == orphanKey);

        var cleanup = new ReportOrphanCleanupService(
            scopes,
            scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>(),
            NullLogger<ReportOrphanCleanupService>.Instance);
        await cleanup.RunCleanupAsync();

        Assert.DoesNotContain(reports.ListStoredFiles(), f => f.StorageKey == orphanKey);
    }
}
