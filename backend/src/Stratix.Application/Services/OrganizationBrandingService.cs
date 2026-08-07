using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
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
    private readonly ITenantFileStorage _files;

    public OrganizationBrandingService(
        IApplicationDbContext db,
        ITenantContext tenant,
        ICurrentUserService currentUser,
        IPlatformCmsService cms,
        ITenantFileStorage files)
    {
        _db = db;
        _tenant = tenant;
        _currentUser = currentUser;
        _cms = cms;
        _files = files;
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

        var key = NormalizeLogoKey(org.Id, org.LogoFileName);
        var opened = await _files.OpenAsync(org.Id, StorageCategories.OrgLogos, key, GuessContentType(org.LogoFileName), ct);
        if (opened is null)
        {
            // Legacy flat layout under storage root only (never path-escape).
            var legacyName = Path.GetFileName(org.LogoFileName);
            if (string.IsNullOrWhiteSpace(legacyName) || legacyName.Contains("..", StringComparison.Ordinal))
                return null;
            var legacy = Path.GetFullPath(Path.Combine(_files.Root, StorageCategories.OrgLogos, legacyName));
            var legacyRoot = Path.GetFullPath(Path.Combine(_files.Root, StorageCategories.OrgLogos));
            if (!legacy.StartsWith(legacyRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(legacy))
                return null;
            Stream stream = File.OpenRead(legacy);
            return (stream, GuessContentType(org.LogoFileName), legacyName);
        }

        return (opened.Value.Stream, opened.Value.ContentType, Path.GetFileName(org.LogoFileName));
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

        var normalizedType = contentType.Split(';', 2)[0].Trim();
        if (!AllowedContentTypes.Contains(normalizedType))
            throw new ArgumentException("Logo must be PNG, JPEG, WebP, or SVG.");

        Stream payload = content;
        MemoryStream? owned = null;
        if (!content.CanSeek)
        {
            owned = new MemoryStream();
            await content.CopyToAsync(owned, ct);
            owned.Position = 0;
            payload = owned;
            length = owned.Length;
        }

        try
        {
            FileSignatureValidator.EnsureMatches(payload, normalizedType);
            if (payload.CanSeek) payload.Position = 0;

            var org = await RequireCurrentOrgAsync(ct);
            var ext = ExtensionFor(normalizedType, originalFileName);
            var fileName = $"org-{org.Id}{ext}";

            if (!string.IsNullOrWhiteSpace(org.LogoFileName))
            {
                _files.Delete(org.Id, StorageCategories.OrgLogos, NormalizeLogoKey(org.Id, org.LogoFileName));
                var legacy = Path.Combine(_files.Root, StorageCategories.OrgLogos, Path.GetFileName(org.LogoFileName));
                if (File.Exists(legacy)) File.Delete(legacy);
            }

            var storageKey = await _files.SaveAsync(org.Id, StorageCategories.OrgLogos, fileName, payload, ct);
            org.LogoFileName = storageKey;
            org.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);

            return new OrganizationLogoUploadResult($"/api/organization/logo?v={org.UpdatedAt.ToUnixTimeSeconds()}");
        }
        finally
        {
            if (owned != null)
                await owned.DisposeAsync();
        }
    }

    public async Task ClearAsync(CancellationToken ct = default)
    {
        if (!await CanUploadAsync(ct))
            throw new UnauthorizedAccessException("Logo upload is not enabled for company admins.");

        var org = await RequireCurrentOrgAsync(ct);
        if (string.IsNullOrWhiteSpace(org.LogoFileName)) return;

        _files.Delete(org.Id, StorageCategories.OrgLogos, NormalizeLogoKey(org.Id, org.LogoFileName));
        var legacy = Path.Combine(_files.Root, StorageCategories.OrgLogos, Path.GetFileName(org.LogoFileName));
        if (File.Exists(legacy)) File.Delete(legacy);

        org.LogoFileName = null;
        org.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private static string NormalizeLogoKey(long organizationId, string stored) =>
        stored.Contains('/') ? stored : $"org-{organizationId}/{Path.GetFileName(stored)}";

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
        return contentType.Split(';', 2)[0].Trim().ToLowerInvariant() switch
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
