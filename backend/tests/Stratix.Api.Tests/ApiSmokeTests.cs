using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Stratix.Api.Tests;

public class ApiSmokeTests : IClassFixture<StratixApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public ApiSmokeTests(StratixApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Health_returns_ok()
    {
        var response = await _client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_token()
    {
        var login = await LoginAsync("admin@stratix.local", "1234");
        Assert.False(string.IsNullOrWhiteSpace(login.Token));
        Assert.NotNull(login.User);
    }

    [Fact]
    public async Task Directory_users_available_to_employee_but_users_admin_forbidden()
    {
        var employee = await LoginAsync("lina.noor@stratix.local", "1234");
        using var directoryRequest = new HttpRequestMessage(HttpMethod.Get, "/api/directory/users");
        directoryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employee.Token);
        var directoryResponse = await _client.SendAsync(directoryRequest);
        Assert.Equal(HttpStatusCode.OK, directoryResponse.StatusCode);

        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, "/api/users");
        adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", employee.Token);
        var adminResponse = await _client.SendAsync(adminRequest);
        Assert.Equal(HttpStatusCode.Forbidden, adminResponse.StatusCode);
    }

    [Fact]
    public async Task Projects_paged_returns_envelope_and_headers()
    {
        var admin = await LoginAsync("admin@stratix.local", "1234");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/projects?page=1&pageSize=2");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin.Token);

        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Total-Count"));
        Assert.True(response.Headers.Contains("X-Page"));

        var body = await response.Content.ReadFromJsonAsync<PagedEnvelope>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(1, body.Page);
        Assert.Equal(2, body.PageSize);
        Assert.True(body.Items.Count <= 2);
        Assert.True(body.Total >= body.Items.Count);
    }

    private async Task<LoginResult> LoginAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResult>(JsonOptions);
        Assert.NotNull(login);
        return login;
    }

    private sealed record LoginResult(string Token, JsonElement? User);
    private sealed record PagedEnvelope(List<JsonElement> Items, int Total, int Page, int PageSize, int TotalPages);
}
