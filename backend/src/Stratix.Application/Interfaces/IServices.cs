using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    long GetExpirationSeconds();
    bool TryValidate(string token, out long userId, out UserRole role);
}

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public interface IPasswordResetMailService
{
    Task SendResetEmailAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}

public interface ICurrentUserService
{
    long? UserId { get; }
    long? OrganizationId { get; }
    string? UserName { get; }
    UserRole? Role { get; }
}

/// <summary>
/// Ambient multi-tenant context for the current request. Resolved from the authenticated
/// user's JWT. Used by the persistence layer to scope every query to a single tenant.
/// </summary>
public interface ITenantContext
{
    /// <summary>Current tenant id, or null for system / anonymous / super-admin callers.</summary>
    long? OrganizationId { get; }

    /// <summary>Platform operator who may act across all tenants.</summary>
    bool IsSuperAdmin { get; }

    /// <summary>True when queries must be filtered to <see cref="OrganizationId"/>
    /// (a normal authenticated user); false for super-admins and system code.</summary>
    bool HasTenantScope { get; }
}
