using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;

namespace Stratix.Api.Tests;

public class SoftDeleteTests : IClassFixture<StratixApiFactory>
{
    private readonly HttpClient _client;

    public SoftDeleteTests(StratixApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Soft_deleted_project_is_hidden_from_get_and_list()
    {
        var admin = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");

        using var createReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", admin.Token, new
        {
            name = $"Temp Delete {Guid.NewGuid():N}",
            description = (string?)null,
            departmentId = (long?)null,
            projectManagerId = (long?)null,
            startDate = (string?)null,
            endDate = (string?)null,
            status = "ACTIVE",
            priority = "LOW",
            progress = 0
        });
        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await TestAuth.ReadJsonAsync<IdDto>(createRes);
        Assert.NotNull(created);

        using var deleteReq = TestAuth.Authed(HttpMethod.Delete, $"/api/projects/{created.Id}", admin.Token);
        var deleteRes = await _client.SendAsync(deleteReq);
        Assert.Equal(HttpStatusCode.NoContent, deleteRes.StatusCode);

        using var getReq = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{created.Id}", admin.Token);
        var getRes = await _client.SendAsync(getReq);
        Assert.Equal(HttpStatusCode.NotFound, getRes.StatusCode);

        using var listReq = TestAuth.Authed(HttpMethod.Get, "/api/projects", admin.Token);
        var listRes = await _client.SendAsync(listReq);
        listRes.EnsureSuccessStatusCode();
        var projects = await TestAuth.ReadJsonAsync<List<IdDto>>(listRes) ?? [];
        Assert.DoesNotContain(projects, p => p.Id == created.Id);
    }

    private sealed record IdDto(long Id);
}

public class PlanAndKpiTests : IClassFixture<StratixApiFactory>
{
    private readonly StratixApiFactory _factory;
    private readonly HttpClient _client;

    public PlanAndKpiTests(StratixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Kpi_duplicate_period_is_rejected()
    {
        var admin = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");
        var period = $"P-{Guid.NewGuid():N}";

        using var dirReq = TestAuth.Authed(HttpMethod.Get, "/api/directory/users", admin.Token);
        var dirRes = await _client.SendAsync(dirReq);
        dirRes.EnsureSuccessStatusCode();
        var users = await TestAuth.ReadJsonAsync<List<IdDto>>(dirRes) ?? [];
        var employee = users.First(u => u.Id != admin.User.Id);

        var payload = new
        {
            userId = employee.Id,
            period,
            tasksCompleted = 5,
            tasksOnTime = 4,
            score = 80,
            notes = "first"
        };

        using var firstReq = TestAuth.Authed(HttpMethod.Post, "/api/employee-kpis", admin.Token, payload);
        var firstRes = await _client.SendAsync(firstReq);
        Assert.Equal(HttpStatusCode.Created, firstRes.StatusCode);

        using var dupReq = TestAuth.Authed(HttpMethod.Post, "/api/employee-kpis", admin.Token, payload);
        var dupRes = await _client.SendAsync(dupReq);
        Assert.Equal(HttpStatusCode.Conflict, dupRes.StatusCode);
        var body = await dupRes.Content.ReadAsStringAsync();
        Assert.Contains("already exists", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Plan_project_limit_is_enforced()
    {
        var email = $"plan-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Limited Org", email);

        await TestAuth.WithDbAsync(_factory, async db =>
        {
            var user = await db.UserSet.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
            var sub = await db.SubscriptionSet.FirstAsync(s => s.OrganizationId == user.OrganizationId);
            sub.PlanCode = "STARTER";
        });

        for (var i = 0; i < 3; i++)
        {
            using var okReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", session.Token, NewProject($"P{i}"));
            var okRes = await _client.SendAsync(okReq);
            Assert.Equal(HttpStatusCode.Created, okRes.StatusCode);
        }

        using var overReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", session.Token, NewProject("Over"));
        var overRes = await _client.SendAsync(overReq);
        Assert.Equal(HttpStatusCode.PaymentRequired, overRes.StatusCode);
        var body = await overRes.Content.ReadAsStringAsync();
        Assert.Contains("allows up to", body, StringComparison.OrdinalIgnoreCase);
    }

    private static object NewProject(string name) => new
    {
        name,
        description = (string?)null,
        departmentId = (long?)null,
        projectManagerId = (long?)null,
        startDate = (string?)null,
        endDate = (string?)null,
        status = "ACTIVE",
        priority = "LOW",
        progress = 0
    };

    private sealed record IdDto(long Id);
}

public class AuthTokenTests : IClassFixture<StratixApiFactory>
{
    private readonly StratixApiFactory _factory;
    private readonly HttpClient _client;

    public AuthTokenTests(StratixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Refresh_token_reuse_revokes_entire_family()
    {
        var email = $"refresh-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Refresh Org", email);
        Assert.False(string.IsNullOrWhiteSpace(session.RefreshToken));

        var first = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var rotated = await TestAuth.ReadJsonAsync<RefreshDto>(first);
        Assert.NotNull(rotated);
        Assert.False(string.IsNullOrWhiteSpace(rotated.RefreshToken));
        Assert.NotEqual(session.RefreshToken, rotated.RefreshToken);

        // Attacker (or stale client) replays the pre-rotation token → family kill.
        var reuse = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);

        // Legitimate post-rotation token is also dead after reuse detection.
        var afterTheft = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = rotated.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, afterTheft.StatusCode);

        await TestAuth.WithDbAsync(_factory, async db =>
        {
            var user = await db.UserSet.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
            var familyTokens = await db.RefreshTokenSet
                .Where(t => t.UserId == user.Id)
                .ToListAsync();
            Assert.NotEmpty(familyTokens);
            Assert.Contains(familyTokens, t => t.ReuseDetectedAt != null);
            Assert.All(familyTokens, t => Assert.NotNull(t.RevokedAt));
        });
    }

    private sealed record RefreshDto(string Token, string? RefreshToken);
}
