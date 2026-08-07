using System.Net;

namespace Stratix.Api.Tests;

public class HealthSnapshotCaptureTests : IClassFixture<StratixApiFactory>
{
    private readonly HttpClient _client;

    public HealthSnapshotCaptureTests(StratixApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Non_formal_capture_skips_identical_same_day_rows()
    {
        var email = $"hsnap-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Health Snap Org", email);

        using var createReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", session.Token, new
        {
            name = $"HS-{Guid.NewGuid():N}",
            description = (string?)null,
            departmentId = (long?)null,
            projectManagerId = (long?)null,
            startDate = (string?)null,
            endDate = (string?)null,
            status = "ACTIVE",
            priority = "LOW",
            progress = 0
        });
        var project = await TestAuth.ReadJsonAsync<IdDto>(await _client.SendAsync(createReq));
        Assert.NotNull(project);

        // Progress recalc after create already may have captured once; force a baseline.
        using var formal1 = TestAuth.Authed(HttpMethod.Post, $"/api/health-snapshots/projects/{project.Id}/capture", session.Token);
        var first = await TestAuth.ReadJsonAsync<SnapDto>(await _client.SendAsync(formal1));
        Assert.NotNull(first);

        using var beforeReq = TestAuth.Authed(HttpMethod.Get, $"/api/health-snapshots/projects/{project.Id}?take=20", session.Token);
        var before = await TestAuth.ReadJsonAsync<List<SnapDto>>(await _client.SendAsync(beforeReq)) ?? [];
        var beforeCount = before.Count;

        // Identical non-formal path: recalculate-progress should not insert another identical row today.
        using var recalc = TestAuth.Authed(HttpMethod.Post, "/api/projects/recalculate-progress", session.Token);
        Assert.Equal(HttpStatusCode.OK, (await _client.SendAsync(recalc)).StatusCode);

        using var afterRecalcReq = TestAuth.Authed(HttpMethod.Get, $"/api/health-snapshots/projects/{project.Id}?take=20", session.Token);
        var afterRecalc = await TestAuth.ReadJsonAsync<List<SnapDto>>(await _client.SendAsync(afterRecalcReq)) ?? [];
        Assert.Equal(beforeCount, afterRecalc.Count);

        // Formal capture always inserts.
        using var formal2 = TestAuth.Authed(HttpMethod.Post, $"/api/health-snapshots/projects/{project.Id}/capture", session.Token);
        var second = await TestAuth.ReadJsonAsync<SnapDto>(await _client.SendAsync(formal2));
        Assert.NotNull(second);
        Assert.NotEqual(first.Id, second.Id);

        using var afterFormalReq = TestAuth.Authed(HttpMethod.Get, $"/api/health-snapshots/projects/{project.Id}?take=20", session.Token);
        var afterFormal = await TestAuth.ReadJsonAsync<List<SnapDto>>(await _client.SendAsync(afterFormalReq)) ?? [];
        Assert.Equal(beforeCount + 1, afterFormal.Count);
    }

    private sealed record IdDto(long Id);
    private sealed record SnapDto(
        long Id, decimal Score, decimal Progress, int OnTimeTasks, int DelayedTasks, int CriticalRisks);
}
