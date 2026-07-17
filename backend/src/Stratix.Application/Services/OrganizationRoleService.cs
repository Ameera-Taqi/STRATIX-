using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Roles;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class OrganizationRoleService : IOrganizationRoleService
{
    private static readonly HashSet<string> AllowedBaseRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(UserRole.ORG_ADMIN),
        nameof(UserRole.ADMIN),
        nameof(UserRole.PROJECT_MANAGER),
        nameof(UserRole.TEAM_LEADER),
        nameof(UserRole.EMPLOYEE),
        nameof(UserRole.EXECUTIVE_VIEWER),
    };

    private readonly IApplicationDbContext _db;

    public OrganizationRoleService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OrganizationRoleResponse>> GetAllAsync(CancellationToken ct = default) =>
        await _db.OrganizationRoles
            .OrderBy(r => r.Name)
            .Select(r => new OrganizationRoleResponse(
                r.Id, r.Code, r.Name, r.Description, r.BaseRole, r.IsSystem, r.CreatedAt))
            .ToListAsync(ct);

    public async Task<OrganizationRoleResponse> CreateAsync(CreateOrganizationRoleRequest request, CancellationToken ct = default)
    {
        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0) throw new ArgumentException("Role name is required.");

        var baseRole = NormalizeBaseRole(request.BaseRole);
        var code = NormalizeCode(string.IsNullOrWhiteSpace(request.Code) ? name : request.Code!);
        if (code.Length == 0) throw new ArgumentException("Role code is required.");
        if (IsReservedCode(code))
            throw new InvalidOperationException("This role code is reserved for a system role.");

        if (await _db.OrganizationRoles.AnyAsync(r => r.Code == code, ct))
            throw new InvalidOperationException("A role with this code already exists.");

        var role = new OrganizationRole
        {
            Name = name,
            Code = code,
            Description = request.Description?.Trim(),
            BaseRole = baseRole,
            IsSystem = false,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        _db.Add(role);
        await _db.SaveChangesAsync(ct);
        return Map(role);
    }

    public async Task<OrganizationRoleResponse> UpdateAsync(long id, UpdateOrganizationRoleRequest request, CancellationToken ct = default)
    {
        var role = await FindAsync(id, ct);
        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be edited.");

        var name = (request.Name ?? string.Empty).Trim();
        if (name.Length == 0) throw new ArgumentException("Role name is required.");

        role.Name = name;
        role.Description = request.Description?.Trim();
        role.BaseRole = NormalizeBaseRole(request.BaseRole);
        await _db.SaveChangesAsync(ct);
        return Map(role);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var role = await FindAsync(id, ct);
        if (role.IsSystem)
            throw new InvalidOperationException("System roles cannot be deleted.");

        if (await _db.Users.AnyAsync(u => u.OrganizationRoleId == id, ct))
            throw new InvalidOperationException("Cannot delete a role that is still assigned to users.");

        _db.Remove(role);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<OrganizationRole> FindAsync(long id, CancellationToken ct) =>
        await _db.OrganizationRoles.FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new KeyNotFoundException("Role not found");

    private static OrganizationRoleResponse Map(OrganizationRole r) =>
        new(r.Id, r.Code, r.Name, r.Description, r.BaseRole, r.IsSystem, r.CreatedAt);

    private static string NormalizeBaseRole(string? raw)
    {
        var value = (raw ?? nameof(UserRole.EMPLOYEE)).Trim().ToUpperInvariant();
        if (!AllowedBaseRoles.Contains(value))
            throw new ArgumentException("Invalid base role. Choose a standard tenant role to inherit.");
        return value;
    }

    private static string NormalizeCode(string raw)
    {
        var sb = new StringBuilder();
        foreach (var ch in raw.Trim().ToUpperInvariant())
        {
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            else if (ch is ' ' or '-' or '_') sb.Append('_');
        }
        var code = Regex.Replace(sb.ToString(), "_+", "_").Trim('_');
        return code.Length > 80 ? code[..80] : code;
    }

    private static bool IsReservedCode(string code) =>
        Enum.TryParse<UserRole>(code, ignoreCase: true, out _);
}
