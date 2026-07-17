using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Branding;
using Stratix.Application.Interfaces;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class OrganizationBrandingService : IOrganizationBrandingService
{
    public const string BrandingModuleCode = "BRANDING";
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/jpg",
        "image/webp",
        "image/svg+xml",
    };
    private const long MaxBytes = 2 * 1024 * 1024;

    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly ICurrentUserService _currentUser;
    private readonly IPlatformCmsService _cms;
    private readonly string _logoRoot;

    public OrganizationBrandingService(
        IApplicationDbContext db,
        ITenantContext tenant,
        ICurrentUserService currentUser,
        IPlatformCmsService cms)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _cms = cms;
        _logoRoot = Path.Combine(AppContext.BaseDirectory, "data", "org-logos");
        Directory.CreateDirectory(_logoRoot);
    }

    public async Task<OrganizationBrandingResponse> GetAsync(CancellationToken ct = default)
    {
        var org = await RequireCurrentOrgAsync(ct);
        return new OrganizationBrandingResponse(
            org.Id,
            org.Name,
            string.IsNullOrWhiteSpace(org.LogoFileName) ? null : $"/api/organization/logo?v={org.UpdatedAt.ToUnixTimeSeconds()}",
            await CanUploadAsync(ct));
    }

    public async Task<(Stream Stream, string ContentType, string FileName)?> OpenLogoAsync(CancellationToken ct = default)
    {
        var org = await RequireCurrentOrgAsync(ct);
        if (string.IsNullOrWhiteSpace(org.LogoFileName)) return null;

        var path = Path.Combine(_logoRoot, org.LogoFileName);
        if (!File.Exists(path)) return null;

        var contentType = GuessContentType(org.LogoFileName);
        Stream stream = File.OpenRead(path);
        return (stream, contentType, org.LogoFileName);
    }

    public async Task<OrganizationLogoUploadResult> UploadAsync(
        Stream content,
        string contentType,
        string originalFileName,
        long length,
        CancellationToken ct = default)
    {
        if (!await CanUploadAsync(ct))
            throw new UnauthorizedAccessException("Logo upload is not enabled for company admins.");

        if (content == null || length <= 0)
            throw new ArgumentException("Logo file is required.");
        if (length > MaxBytes)
            throw new ArgumentException("Logo must be 2 MB or smaller.");
        if (!AllowedContentTypes.Contains(contentType))
            throw new ArgumentException("Logo must be PNG, JPEG, WebP, or SVG.");

        var org = await RequireCurrentOrgAsync(ct);
        var ext = ExtensionFor(contentType, originalFileName);
        var fileName = $"org-{org.Id}{ext}";
        var path = Path.Combine(_logoRoot, fileName);

        if (!string.IsNullOrWhiteSpace(org.LogoFileName) &&
            !string.Equals(org.LogoFileName, fileName, StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(_logoRoot, org.LogoFileName);
            if (File.Exists(oldPath)) File.Delete(oldPath);
        }

        await using (var fs = File.Create(path))
        {
            await content.CopyToAsync(fs, ct);
        }

        org.LogoFileName = fileName;
        org.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new OrganizationLogoUploadResult($"/api/organization/logo?v={org.UpdatedAt.ToUnixTimeSeconds()}");
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        if (!await CanUploadAsync(ct))
            throw new UnauthorizedAccessException("Logo upload is not enabled for company admins.");

        var org = await RequireCurrentOrgAsync(ct);
        if (string.IsNullOrWhiteSpace(org.LogoFileName)) return;

        var path = Path.Combine(_logoRoot, org.LogoFileName);
        if (File.Exists(path)) File.Delete(path);

        org.LogoFileName = null;
        org.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<bool> CanUploadAsync(CancellationToken ct)
    {
        var role = _currentUser.Role;
        if (role == UserRole.SUPER_ADMIN) return true;
        if (role is not (UserRole.ORG_ADMIN or UserRole.ADMIN)) return false;

        var permissions = await _cms.GetAllAsync(ct);
        var branding = permissions.FirstOrDefault(p =>
            string.Equals(p.ModuleCode, BrandingModuleCode, StringComparison.OrdinalIgnoreCase));
        return branding is { VisibleToCompanyAdmin: true, WritableByCompanyAdmin: true };
    }

    private async Task<Domain.Entities.Organization> RequireCurrentOrgAsync(CancellationToken ct)
    {
        if (_tenant.OrganizationId is not long orgId)
            throw new UnauthorizedAccessException("No organization context.");
        return await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct)
            ?? throw new KeyNotFoundException("Organization not found.");
    }

    private static string ExtensionFor(string contentType, string originalName)
    {
        return contentType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => Path.GetExtension(originalName) is { Length: > 0 } e ? e.ToLowerInvariant() : ".png",
        };
    }

    private static string GuessContentType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream",
        };
}
