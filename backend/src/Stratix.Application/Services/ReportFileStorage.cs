using Stratix.Application.Interfaces;

namespace Stratix.Application.Services;

/// <summary>
/// Report-specific policy (MIME / size) over the shared <see cref="ITenantFileStorage"/>.
/// Storage keys remain <c>org-{id}/file</c> under the reports category.
/// </summary>
public class ReportFileStorage : IReportFileStorage
{
    public const long DefaultMaxBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/csv",
        "application/csv",
        "text/plain",
        "text/html",
    };

    private readonly ITenantFileStorage _files;

    public ReportFileStorage(ITenantFileStorage files) => _files = files;

    public long MaxBytes => DefaultMaxBytes;
    public IReadOnlySet<string> AllowedContentTypes => Allowed;

    public void Validate(string contentType, long length, string format)
    {
        if (length <= 0) throw new ArgumentException("Report file is required.");
        if (length > MaxBytes)
            throw new ArgumentException($"Report file must be {MaxBytes / (1024 * 1024)} MB or smaller.");

        var normalized = (contentType ?? "").Split(';', 2)[0].Trim();
        if (!Allowed.Contains(normalized))
            throw new ArgumentException("Report file must be PDF, Excel, or CSV.");

        var fmt = (format ?? "").Trim().ToUpperInvariant();
        if (fmt is not ("PDF" or "EXCEL"))
            throw new ArgumentException("Format must be PDF or EXCEL.");

        if (fmt == "PDF" &&
            !normalized.Equals("application/pdf", StringComparison.OrdinalIgnoreCase) &&
            !normalized.Equals("text/html", StringComparison.OrdinalIgnoreCase) &&
            !normalized.Equals("text/plain", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("PDF format requires PDF or printable HTML/text content.");

        if (fmt == "EXCEL" && normalized.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("EXCEL format cannot use PDF content type.");
    }

    public Task<string> SaveAsync(long organizationId, string storageFileName, Stream content, CancellationToken ct = default) =>
        _files.SaveAsync(organizationId, StorageCategories.Reports, storageFileName, content, ct);

    public Task<(Stream Stream, string ContentType)?> OpenAsync(
        long organizationId,
        string storageKey,
        string? contentType,
        CancellationToken ct = default) =>
        _files.OpenAsync(organizationId, StorageCategories.Reports, storageKey, contentType, ct);

    public void Delete(long organizationId, string storageKey) =>
        _files.Delete(organizationId, StorageCategories.Reports, storageKey);

    public IReadOnlyList<(long OrganizationId, string StorageKey)> ListStoredFiles() =>
        _files.ListStoredFiles(StorageCategories.Reports);
}
