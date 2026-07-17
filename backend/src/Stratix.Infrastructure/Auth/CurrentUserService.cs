using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Stratix.Application.Interfaces;
using Stratix.Domain.Enums;

namespace Stratix.Infrastructure.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    public long? UserId
    {
        get
        {
            var id = _http.HttpContext?.User.FindFirst("userId")?.Value
                ?? _http.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(id, out var userId) ? userId : null;
        }
    }

    public long? OrganizationId
    {
        get
        {
            var orgId = _http.HttpContext?.User.FindFirst("orgId")?.Value;
            return long.TryParse(orgId, out var id) ? id : null;
        }
    }

    public string? UserName => _http.HttpContext?.User.FindFirst("name")?.Value
        ?? _http.HttpContext?.User.FindFirst(ClaimTypes.Name)?.Value;

    public UserRole? Role
    {
        get
        {
            var role = _http.HttpContext?.User.FindFirst("role")?.Value
                ?? _http.HttpContext?.User.FindFirst(ClaimTypes.Role)?.Value;
            return role != null && Enum.TryParse<UserRole>(role, out var r) ? r : null;
        }
    }
}
