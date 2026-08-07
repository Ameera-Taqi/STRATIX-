using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Cms;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;

namespace Stratix.Application.Services;

public class PlatformCmsService : IPlatformCmsService
{
    /// <summary>Modules company admins may be granted via CMS (excludes platform-only modules).</summary>
    public static readonly (string Code, int Order, bool DefaultVisible, bool DefaultWritable)[] ManagedModules =
    [
        ("DASHBOARD", 1, true, true),
        ("PROJECTS", 2, true, true),
        ("STAGES", 3, true, true),
        ("TASKS", 4, true, true),
        ("EMPLOYEES", 5, true, true),
        ("PERFORMANCE", 6, true, true),
        ("REPORTS", 7, true, true),
        ("NOTIFICATIONS", 8, true, true),
        ("RISKS", 9, true, true),
        ("AUDIT", 10, true, false),
        ("SETTINGS", 11, true, true),
        ("ROLES", 12, true, true),
        ("BRANDING", 13, true, false),
        ("DEPARTMENTS", 14, true, true),
    ];

    private readonly IApplicationDbContext _db;

    public PlatformCmsService(IApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<CompanyAdminModulePermissionResponse>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureDefaultsAsync(ct);
        return await _db.PlatformModulePermissions
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.ModuleCode)
            .Select(p => ToResponse(p))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<CompanyAdminModulePermissionResponse>> UpdateAsync(
        UpdateCompanyAdminPermissionsRequest request,
        CancellationToken ct = default)
    {
        await EnsureDefaultsAsync(ct);
        var allowed = ManagedModules.Select(m => m.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var now = DateTimeOffset.UtcNow;
        var existing = await _db.PlatformModulePermissions.ToListAsync(ct);

        foreach (var item in request.Modules)
        {
            if (!allowed.Contains(item.ModuleCode))
                throw new ArgumentException($"Module '{item.ModuleCode}' cannot be managed in CMS.");

            var row = existing.FirstOrDefault(p =>
                string.Equals(p.ModuleCode, item.ModuleCode, StringComparison.OrdinalIgnoreCase));
            if (row == null) continue;

            row.VisibleToCompanyAdmin = item.VisibleToCompanyAdmin;
            // Write requires visibility.
            row.WritableByCompanyAdmin = item.VisibleToCompanyAdmin && item.WritableByCompanyAdmin;
            row.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
        return await GetAllAsync(ct);
    }

    public async Task EnsureDefaultsAsync(CancellationToken ct = default)
    {
        var existingCodes = await _db.PlatformModulePermissions.Select(p => p.ModuleCode).ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        var added = false;

        foreach (var (code, order, visible, writable) in ManagedModules)
        {
            if (existingCodes.Any(c => string.Equals(c, code, StringComparison.OrdinalIgnoreCase)))
                continue;

            _db.Add(new PlatformModulePermission
            {
                ModuleCode = code,
                VisibleToCompanyAdmin = visible,
                WritableByCompanyAdmin = writable,
                SortOrder = order,
                UpdatedAt = now,
            });
            added = true;
        }

        if (added)
            await _db.SaveChangesAsync(ct);
    }

    private static CompanyAdminModulePermissionResponse ToResponse(PlatformModulePermission p) =>
        new(p.Id, p.ModuleCode, p.VisibleToCompanyAdmin, p.WritableByCompanyAdmin, p.SortOrder, p.UpdatedAt);
}
