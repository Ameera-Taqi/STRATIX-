using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Stratix.Domain.Enums;

namespace Stratix.Api.Tests;

public class OrganizationAccessTests : IClassFixture<StratixApiFactory>
{
    private readonly StratixApiFactory _factory;
    private readonly HttpClient _client;

    public OrganizationAccessTests(StratixApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Expired_trial_cannot_refresh()
    {
        var email = $"trial-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Trial Org", email);
        Assert.False(string.IsNullOrWhiteSpace(session.RefreshToken));

        await TestAuth.WithDbAsync(_factory, async db =>
        {
            var user = await db.UserSet.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
            var sub = await db.SubscriptionSet.FirstAsync(s => s.OrganizationId == user.OrganizationId);
            sub.Status = SubscriptionStatus.TRIALING;
            sub.TrialEndsAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        });

        var refresh = await _client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = session.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    [Fact]
    public async Task Suspended_organization_cannot_login_or_call_protected_endpoints()
    {
        var email = $"susp-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Suspended Org", email);

        // Token still valid until we suspend — prove protected call works first.
        using var okReq = TestAuth.Authed(HttpMethod.Get, "/api/projects", session.Token);
        var okRes = await _client.SendAsync(okReq);
        Assert.Equal(HttpStatusCode.OK, okRes.StatusCode);

        await TestAuth.WithDbAsync(_factory, async db =>
        {
            var user = await db.UserSet.IgnoreQueryFilters().FirstAsync(u => u.Email == email);
            var org = await db.OrganizationSet.FirstAsync(o => o.Id == user.OrganizationId);
            org.Status = OrganizationStatus.SUSPENDED;
        });

        var login = await _client.PostAsJsonAsync("/api/auth/login", new { username = email, password = "Secret123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        using var blockedReq = TestAuth.Authed(HttpMethod.Get, "/api/projects", session.Token);
        var blockedRes = await _client.SendAsync(blockedReq);
        Assert.Equal(HttpStatusCode.Unauthorized, blockedRes.StatusCode);
    }
}
