using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Stratix.Application.Interfaces;

namespace Stratix.Infrastructure.Auth;

/// <summary>
/// Resolves the current tenant from the authenticated user's JWT claims (per request).
/// </summary>
public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _http;

    public TenantContext(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;
    private bool Authenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsSuperAdmin =>
        Authenticated &&
        (User!.FindFirst("role")?.Value == "SUPER_ADMIN" || User.FindFirst(ClaimTypes.Role)?.Value == "SUPER_ADMIN");

    public long? OrganizationId =>
        long.TryParse(User?.FindFirst("orgId")?.Value, out var id) ? id : null;

    public bool HasTenantScope => Authenticated && !IsSuperAdmin;
}
