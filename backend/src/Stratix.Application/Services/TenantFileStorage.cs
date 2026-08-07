using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stratix.Application.Interfaces;
using Stratix.Application.Observability;

namespace Stratix.Application.Services;

/// <summary>
/// Shared disk storage: <c>{Root}/{category}/org-{id}/{file}</c>.
/// Root from <c>Stratix:Storage:Root</c> (default <c>data/</c> under the app base directory).
/// Never expose this root via static file middleware — serve only through authenticated APIs.
/// </summary>
public class TenantFileStorage : ITenantFileStorage
{
    private readonly string _root;
    private readonly IStratixMetrics _metrics;
    private readonly ILogger<TenantFileStorage> _logger;

    public TenantFileStorage(
        IConfiguration configuration,
        IStratixMetrics metrics,
        ILogger<TenantFileStorage> logger)
    {
        _metrics = metrics;
        _logger = logger;
        var configured = configuration["Stratix:Storage:Root"];
        _root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "data")
            : Path.IsPathRooted(configured)
                ? configured
                : Path.Combine(AppContext.BaseDirectory, configured);
        Directory.CreateDirectory(_root);
    }

    public string Root => _root;

    public async Task<string> SaveAsync(
        long organizationId,
        string category,
        string storageFileName,
        Stream content,
        CancellationToken ct = default)
    {
        if (organizationId <= 0) throw new ArgumentException("OrganizationId is required.");
        var cat = NormalizeCategory(category);
        var safeName = SanitizeFileName(storageFileName);
        var orgDir = GetOrgDirectory(organizationId, cat);

        try
        {
            Directory.CreateDirectory(orgDir);
            var fullPath = Path.Combine(orgDir, safeName);
            await using (var fs = File.Create(fullPath))
            {
                await content.CopyToAsync(fs, ct);
            }

            return Path.Combine($"org-{organizationId}", safeName).Replace('\\', '/');
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or DirectoryNotFoundException)
        {
            Fail("save", ex);
            throw;
        }
    }

    public Task<(Stream Stream, string ContentType)?> OpenAsync(
        long organizationId,
        string category,
        string storageKey,
        string? contentType,
        CancellationToken ct = default)
    {
        try
        {
            var path = ResolvePath(organizationId, category, storageKey);
            if (path == null || !File.Exists(path)) return Task.FromResult<(Stream, string)?>(null);

            Stream stream = File.OpenRead(path);
            var type = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType;
            return Task.FromResult<(Stream, string)?>((stream, type));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("open", ex);
            throw;
        }
    }

    public void Delete(long organizationId, string category, string storageKey)
    {
        try
        {
            var path = ResolvePath(organizationId, category, storageKey);
            if (path != null && File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("delete", ex);
            throw;
        }
    }

    public IReadOnlyList<(long OrganizationId, string StorageKey)> ListStoredFiles(string category)
    {
        try
        {
            var cat = NormalizeCategory(category);
            var results = new List<(long, string)>();
            var categoryRoot = Path.Combine(_root, cat);
            if (!Directory.Exists(categoryRoot)) return results;

            foreach (var orgDir in Directory.EnumerateDirectories(categoryRoot, "org-*"))
            {
                var name = Path.GetFileName(orgDir);
                if (name is null || !name.StartsWith("org-", StringComparison.OrdinalIgnoreCase)) continue;
                if (!long.TryParse(name.AsSpan(4), out var orgId) || orgId <= 0) continue;

                foreach (var file in Directory.EnumerateFiles(orgDir))
                {
                    var fileName = Path.GetFileName(file);
                    if (string.IsNullOrWhiteSpace(fileName)) continue;
                    results.Add((orgId, $"org-{orgId}/{fileName}".Replace('\\', '/')));
                }
            }

            return results;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Fail("list", ex);
            throw;
        }
    }

    public string SanitizeFileName(string name)
    {
        var baseName = Path.GetFileName(name.Trim());
        if (string.IsNullOrWhiteSpace(baseName)) baseName = "file.bin";
        foreach (var c in Path.GetInvalidFileNameChars())
            baseName = baseName.Replace(c, '_');
        if (baseName.Length > 180) baseName = baseName[..180];
        return baseName;
    }

    private void Fail(string operation, Exception ex)
    {
        var reason = ex.GetType().Name;
        _metrics.RecordStorageFailure(operation, reason);
        // Log type + operation only — never file contents or full paths with secrets.
        _logger.LogError(ex, "Storage {Operation} failed ({Reason})", operation, reason);
    }

    private string GetOrgDirectory(long organizationId, string category) =>
        Path.Combine(_root, category, $"org-{organizationId}");

    private string? ResolvePath(long organizationId, string category, string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;
        var cat = NormalizeCategory(category);
        var expectedPrefix = $"org-{organizationId}/";
        var key = storageKey.Replace('\\', '/').TrimStart('/');
        if (!key.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            return null;

        var fileName = Path.GetFileName(key);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains("..", StringComparison.Ordinal))
            return null;

        var full = Path.GetFullPath(Path.Combine(GetOrgDirectory(organizationId, cat), fileName));
        var orgRoot = Path.GetFullPath(GetOrgDirectory(organizationId, cat));
        if (!full.StartsWith(orgRoot, StringComparison.OrdinalIgnoreCase))
            return null;
        return full;
    }

    private static string NormalizeCategory(string category)
    {
        var cat = (category ?? "").Trim().Trim('/').ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(cat) || cat.Contains("..", StringComparison.Ordinal) || cat.Contains('/') || cat.Contains('\\'))
            throw new ArgumentException("Invalid storage category.");
        return cat;
    }
}
