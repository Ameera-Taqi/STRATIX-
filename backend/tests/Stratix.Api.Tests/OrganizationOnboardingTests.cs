using System.Net;
using System.Net.Http.Json;

namespace Stratix.Api.Tests;

public class OrganizationOnboardingTests : IClassFixture<StratixApiFactory>
{
    private readonly HttpClient _client;

    public OrganizationOnboardingTests(StratixApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task New_org_admin_starts_onboarding_then_can_complete()
    {
        var email = $"onboard-{Guid.NewGuid():N}@test.local";
        var session = await TestAuth.RegisterOrgAsync(_client, "Onboard Co", email);

        using var statusReq = TestAuth.Authed(HttpMethod.Get, "/api/organization/onboarding", session.Token);
        var statusRes = await _client.SendAsync(statusReq);
        Assert.Equal(HttpStatusCode.OK, statusRes.StatusCode);
        var status = await statusRes.Content.ReadFromJsonAsync<OnboardingDto>();
        Assert.NotNull(status);
        Assert.False(status.OnboardingCompleted);
        Assert.Equal("Onboard Co", status.OrganizationName);

        using var profileReq = TestAuth.Authed(HttpMethod.Put, "/api/organization/profile", session.Token, new
        {
            organizationName = "Onboard Co",
            industry = "Technology",
            timezone = "Asia/Riyadh",
            preferredLanguage = "ar"
        });
        var profileRes = await _client.SendAsync(profileReq);
        Assert.Equal(HttpStatusCode.OK, profileRes.StatusCode);
        var updated = await profileRes.Content.ReadFromJsonAsync<OnboardingDto>();
        Assert.Equal("Technology", updated!.Industry);
        Assert.Equal("ar", updated.PreferredLanguage);

        using var completeReq = TestAuth.Authed(HttpMethod.Post, "/api/organization/onboarding/complete", session.Token);
        var completeRes = await _client.SendAsync(completeReq);
        Assert.Equal(HttpStatusCode.OK, completeRes.StatusCode);
        var done = await completeRes.Content.ReadFromJsonAsync<OnboardingDto>();
        Assert.True(done!.OnboardingCompleted);
    }

    private sealed record OnboardingDto(
        long OrganizationId,
        string OrganizationName,
        string? Industry,
        string? PreferredLanguage,
        bool OnboardingCompleted);
}
