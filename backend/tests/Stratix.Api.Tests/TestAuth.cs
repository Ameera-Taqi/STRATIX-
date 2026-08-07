using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Stratix.Infrastructure.Persistence;

namespace Stratix.Api.Tests;

internal static class TestAuth
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<AuthSession> LoginAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { username, password });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Login failed ({(int)response.StatusCode}): {body}");

        var login = JsonSerializer.Deserialize<LoginDto>(body, JsonOptions)
            ?? throw new InvalidOperationException("Login returned empty body.");
        return new AuthSession(login.Token, login.RefreshToken, login.User);
    }

    public static async Task<AuthSession> RegisterOrgAsync(
        HttpClient client,
        string orgName,
        string adminEmail,
        string password = "Secret123!")
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            organizationName = orgName,
            adminName = $"{orgName} Admin",
            adminEmail,
            password
        });
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Register failed ({(int)response.StatusCode}): {body}");

        var login = JsonSerializer.Deserialize<LoginDto>(body, JsonOptions)
            ?? throw new InvalidOperationException("Register returned empty body.");
        return new AuthSession(login.Token, login.RefreshToken, login.User);
    }

    public static HttpRequestMessage Authed(HttpMethod method, string url, string token, object? jsonBody = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (jsonBody is not null)
            request.Content = JsonContent.Create(jsonBody);
        return request;
    }

    public static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(body)) return default;
        return JsonSerializer.Deserialize<T>(body, JsonOptions);
    }

    public static void WithDb(StratixApiFactory factory, Action<StratixDbContext> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StratixDbContext>();
        action(db);
        db.SaveChanges();
    }

    public static async Task WithDbAsync(StratixApiFactory factory, Func<StratixDbContext, Task> action)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StratixDbContext>();
        await action(db);
        await db.SaveChangesAsync();
    }

    private sealed record LoginDto(string Token, string? RefreshToken, UserDto User);
    internal sealed record UserDto(long Id, string Name, string Email, string Role, string? RoleCode = null);
}

internal sealed record AuthSession(string Token, string? RefreshToken, TestAuth.UserDto User);
