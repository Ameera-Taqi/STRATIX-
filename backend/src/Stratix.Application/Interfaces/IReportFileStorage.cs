namespace Stratix.Application.Interfaces;

/// <summary>
/// Tenant-scoped report file storage policy.
/// Files live under <c>data/reports/org-{orgId}/</c>; only PDF and Excel-family types are allowed.
/// </summary>
public interface IReportFileStorage
{
    long MaxBytes { get; }
    IReadOnlySet<string> AllowedContentTypes { get; }

    void Validate(Stream content, string contentType, long length, string format);
    Task<string> SaveAsync(long organizationId, string storageFileName, Stream content, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)?> OpenAsync(long organizationId, string storageKey, string? contentType, CancellationToken ct = default);
    void Delete(long organizationId, string storageKey);

    /// <summary>Enumerate relative storage keys under the reports root (org-{id}/file).</summary>
    IReadOnlyList<(long OrganizationId, string StorageKey)> ListStoredFiles();
}
