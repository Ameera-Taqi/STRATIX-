using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Stratix.Api.Tests;

/// <summary>SaaS tenant isolation — the highest-priority security suite.</summary>
public class TenantIsolationTests : IClassFixture<StratixApiFactory>
{
    private readonly StratixApiFactory _factory;
    private readonly HttpClient _client;

    public TenantIsolationTests(StratixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Tenant_A_cannot_read_Tenant_B_project()
    {
        var a = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");
        var b = await TestAuth.RegisterOrgAsync(_client, "Tenant B Co", $"b-{Guid.NewGuid():N}@test.local");

        using var createReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", b.Token, new
        {
            name = "Secret B Project",
            description = "belongs to B",
            departmentId = (long?)null,
            projectManagerId = (long?)null,
            startDate = (string?)null,
            endDate = (string?)null,
            status = "ACTIVE",
            priority = "MEDIUM",
            progress = 0
        });
        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.Created, createRes.StatusCode);
        var created = await TestAuth.ReadJsonAsync<IdDto>(createRes);
        Assert.NotNull(created);

        using var readAsA = TestAuth.Authed(HttpMethod.Get, $"/api/projects/{created.Id}", a.Token);
        var readRes = await _client.SendAsync(readAsA);
        Assert.Equal(HttpStatusCode.NotFound, readRes.StatusCode);

        using var listAsA = TestAuth.Authed(HttpMethod.Get, "/api/projects", a.Token);
        var listRes = await _client.SendAsync(listAsA);
        listRes.EnsureSuccessStatusCode();
        var projects = await TestAuth.ReadJsonAsync<List<IdDto>>(listRes) ?? [];
        Assert.DoesNotContain(projects, p => p.Id == created.Id);
    }

    [Fact]
    public async Task Tenant_A_cannot_reference_Tenant_B_user_as_project_manager()
    {
        var a = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");
        var b = await TestAuth.RegisterOrgAsync(_client, "Tenant B Refs", $"bref-{Guid.NewGuid():N}@test.local");

        using var createReq = TestAuth.Authed(HttpMethod.Post, "/api/projects", a.Token, new
        {
            name = "Cross-tenant attempt",
            description = (string?)null,
            departmentId = (long?)null,
            projectManagerId = b.User.Id,
            startDate = (string?)null,
            endDate = (string?)null,
            status = "ACTIVE",
            priority = "LOW",
            progress = 0
        });
        var createRes = await _client.SendAsync(createReq);
        Assert.Equal(HttpStatusCode.BadRequest, createRes.StatusCode);
        var body = await createRes.Content.ReadAsStringAsync();
        Assert.Contains("User does not belong to this organization", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Tenant_A_directory_does_not_list_Tenant_B_users()
    {
        var a = await TestAuth.LoginAsync(_client, "admin@stratix.local", "1234");
        var b = await TestAuth.RegisterOrgAsync(_client, "Hidden Org", $"hidden-{Guid.NewGuid():N}@test.local");

        using var dirReq = TestAuth.Authed(HttpMethod.Get, "/api/directory/users", a.Token);
        var dirRes = await _client.SendAsync(dirReq);
        dirRes.EnsureSuccessStatusCode();
        var users = await TestAuth.ReadJsonAsync<List<IdDto>>(dirRes) ?? [];
        Assert.DoesNotContain(users, u => u.Id == b.User.Id);
    }

    private sealed record IdDto(long Id);
}
