namespace Stratix.Application.Interfaces;

/// <summary>Well-known storage categories under the shared tenant file root.</summary>
public static class StorageCategories
{
    public const string Reports = "reports";
    public const string OrgLogos = "org-logos";
    public const string ProjectFiles = "project-files";
}

/// <summary>
/// Unified tenant-scoped file storage under <c>{Root}/{category}/org-{orgId}/</c>.
/// </summary>
public interface ITenantFileStorage
{
    string Root { get; }

    Task<string> SaveAsync(long organizationId, string category, string storageFileName, Stream content, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)?> OpenAsync(long organizationId, string category, string storageKey, string? contentType, CancellationToken ct = default);
    void Delete(long organizationId, string category, string storageKey);
    IReadOnlyList<(long OrganizationId, string StorageKey)> ListStoredFiles(string category);
    string SanitizeFileName(string name);
}
