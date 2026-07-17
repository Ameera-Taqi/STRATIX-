using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Stratix.Application.DTOs.Auth;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class AuthService : IAuthService
{
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private static readonly Dictionary<string, string> LoginAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["superadmin"] = "superadmin@stratix.local",
        ["super"] = "superadmin@stratix.local",
        ["admin"] = "admin@stratix.local",
        ["sara"] = "sara.ali@stratix.local",
        ["employee"] = "lina.noor@stratix.local"
    };

    private readonly IApplicationDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly ICurrentUserService _currentUser;

    public AuthService(IApplicationDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwt, ICurrentUserService currentUser)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _currentUser = currentUser;
    }

    public async Task<LoginResponse> RegisterOrganizationAsync(RegisterOrganizationRequest request, CancellationToken ct = default)
    {
        var name = request.OrganizationName?.Trim() ?? "";
        var adminName = request.AdminName?.Trim() ?? "";
        var email = request.AdminEmail?.Trim().ToLowerInvariant() ?? "";

        if (name.Length == 0 || adminName.Length == 0 || email.Length == 0 || string.IsNullOrWhiteSpace(request.Password))
            throw new ArgumentException("Organization name, admin name, email and password are required.");
        if (await _db.Users.AnyAsync(u => u.Email == email, ct))
            throw new InvalidOperationException("Email already in use.");

        var now = DateTimeOffset.UtcNow;

        // Organization + first ORG_ADMIN + trial subscription are created atomically:
        // if any part fails, nothing is persisted (no half-registered tenant).
        await using var tx = await _db.BeginTransactionAsync(ct);
        try
        {
            var org = new Organization
            {
                Name = name,
                Slug = await GenerateUniqueSlugAsync(request.Slug, name, ct),
                Status = OrganizationStatus.ACTIVE,
                SubscriptionPlan = SubscriptionPlan.FREE,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Add(org);
            await _db.SaveChangesAsync(ct);

            var admin = new User
            {
                OrganizationId = org.Id,
                Name = adminName,
                Email = email,
                Password = _passwordHasher.Hash(request.Password),
                Role = UserRole.ORG_ADMIN,
                JobTitle = "Organization Administrator",
                Status = UserStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Add(admin);

            var subscription = new Subscription
            {
                OrganizationId = org.Id,
                PlanCode = "TRIAL",
                Status = SubscriptionStatus.TRIALING,
                StartedAt = now,
                TrialEndsAt = now.AddDays(14),
                CreatedAt = now
            };
            _db.Add(subscription);
            await _db.SaveChangesAsync(ct);

            var refresh = await IssueRefreshTokenAsync(admin.Id, ct);
            await tx.CommitAsync(ct);
            return new LoginResponse(_jwt.GenerateToken(admin), _jwt.GetExpirationSeconds(), EntityMappers.ToProfile(admin), refresh);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private async Task<string> GenerateUniqueSlugAsync(string? requestedSlug, string name, CancellationToken ct)
    {
        var source = !string.IsNullOrWhiteSpace(requestedSlug) ? requestedSlug! : name;
        var baseSlug = new string(source.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray())
            .Trim('-');
        while (baseSlug.Contains("--")) baseSlug = baseSlug.Replace("--", "-");
        if (baseSlug.Length == 0) baseSlug = "org";

        var slug = baseSlug;
        var suffix = 1;
        while (await _db.Organizations.AnyAsync(o => o.Slug == slug, ct))
            slug = $"{baseSlug}-{++suffix}";
        return slug;
    }

    public async Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await ResolveUserAsync(username, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        if (user.LockoutUntil is { } until && until > DateTimeOffset.UtcNow)
            throw new UnauthorizedAccessException("Account temporarily locked due to repeated failed attempts. Try again later.");

        if (user.Status != UserStatus.ACTIVE || !_passwordHasher.Verify(password, user.Password))
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= MaxFailedAttempts)
            {
                user.LockoutUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
                user.FailedLoginAttempts = 0;
            }
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (user.FailedLoginAttempts != 0 || user.LockoutUntil != null)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutUntil = null;
            await _db.SaveChangesAsync(ct);
        }

        await EnsureOrganizationActiveAsync(user, ct);

        var refresh = await IssueRefreshTokenAsync(user.Id, ct);
        return new LoginResponse(_jwt.GenerateToken(user), _jwt.GetExpirationSeconds(), EntityMappers.ToProfile(user), refresh);
    }

    // Platform owners (SUPER_ADMIN) are exempt — they are attached to a tenant only for FK
    // integrity and must always be able to sign in regardless of that tenant's billing state.
    private async Task EnsureOrganizationActiveAsync(User user, CancellationToken ct)
    {
        if (user.Role == UserRole.SUPER_ADMIN) return;

        var org = await _db.Organizations.FirstOrDefaultAsync(o => o.Id == user.OrganizationId, ct);
        if (org == null) return;

        if (org.Status is OrganizationStatus.SUSPENDED or OrganizationStatus.CANCELLED)
            throw new UnauthorizedAccessException($"Your organization's account is {org.Status.ToString().ToLowerInvariant()}. Contact your administrator or support.");

        var subscription = await _db.Subscriptions.FirstOrDefaultAsync(s => s.OrganizationId == user.OrganizationId, ct);
        if (subscription?.Status == SubscriptionStatus.CANCELLED)
            throw new UnauthorizedAccessException("Your organization's subscription has been cancelled. Contact your administrator or support.");
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = HashToken(refreshToken ?? "");
        var token = await _db.RefreshTokens.Include(t => t.User).ThenInclude(u => u.Department)
            .FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null && t.ExpiresAt > DateTimeOffset.UtcNow, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token");

        if (token.User.Status != UserStatus.ACTIVE)
            throw new UnauthorizedAccessException("Invalid refresh token");

        // Rotate: the presented token is single-use.
        token.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var newRefresh = await IssueRefreshTokenAsync(token.User.Id, ct);
        return new LoginResponse(_jwt.GenerateToken(token.User), _jwt.GetExpirationSeconds(), EntityMappers.ToProfile(token.User), newRefresh);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;
        var hash = HashToken(refreshToken);
        var token = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.RevokedAt == null, ct);
        if (token != null)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<string> IssueRefreshTokenAsync(long userId, CancellationToken ct)
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _db.Add(new RefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(raw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(ct);
        return raw;
    }

    private static string HashToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();

    public async Task<AuthUserProfile> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var user = await GetCurrentUserEntityAsync(ct);
        return EntityMappers.ToProfile(user);
    }

    public async Task<AuthUserProfile> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken ct = default)
    {
        var user = await GetCurrentUserEntityAsync(ct);
        user.Name = request.Name.Trim();
        user.Email = request.Email.Trim().ToLowerInvariant();
        user.UpdatedAt = DateTimeOffset.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            var dept = await _db.Departments.FirstOrDefaultAsync(d => d.Name == request.Department.Trim(), ct);
            user.DepartmentId = dept?.Id;
        }

        await _db.SaveChangesAsync(ct);
        await _db.Users.Where(u => u.Id == user.Id).Select(u => u.Department).LoadAsync(ct);
        return EntityMappers.ToProfile(user);
    }

    private async Task<User> GetCurrentUserEntityAsync(CancellationToken ct)
    {
        if (_currentUser.UserId is not long userId)
            throw new UnauthorizedAccessException("Not authenticated");

        return await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedAccessException("User not found");
    }

    private async Task<User?> ResolveUserAsync(string username, CancellationToken ct)
    {
        var trimmed = username.Trim();
        var lower = trimmed.ToLowerInvariant();

        if (LoginAliases.TryGetValue(lower, out var aliasEmail))
        {
            var byAlias = await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Email == aliasEmail, ct);
            if (byAlias != null) return byAlias;
            return await ResolveAliasFallbackAsync(lower, ct);
        }

        return await _db.Users.Include(u => u.Department)
            .FirstOrDefaultAsync(u => u.Email == lower, ct);
    }

    private async Task<User?> ResolveAliasFallbackAsync(string alias, CancellationToken ct) => alias switch
    {
        "superadmin" or "super" => await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Role == UserRole.SUPER_ADMIN, ct),
        "admin" => await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Role == UserRole.ADMIN, ct),
        "sara" => await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Role == UserRole.PROJECT_MANAGER, ct),
        "employee" => await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Role == UserRole.EMPLOYEE, ct),
        _ => null
    };
}
