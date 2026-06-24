using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Auth;
using Stratix.Application.Interfaces;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class AuthService : IAuthService
{
    private static readonly Dictionary<string, string> LoginAliases = new(StringComparer.OrdinalIgnoreCase)
    {
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

    public async Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await ResolveUserAsync(username, ct)
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        if (user.Status != UserStatus.ACTIVE || !_passwordHasher.Verify(password, user.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        return new LoginResponse(_jwt.GenerateToken(user), _jwt.GetExpirationSeconds(), EntityMappers.ToProfile(user));
    }

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
        "admin" => await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Role == UserRole.ADMIN, ct),
        "sara" => await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Role == UserRole.PROJECT_MANAGER, ct),
        "employee" => await _db.Users.Include(u => u.Department).FirstOrDefaultAsync(u => u.Role == UserRole.EMPLOYEE, ct),
        _ => null
    };
}
