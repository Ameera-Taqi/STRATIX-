using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Reports;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public class ReportService : IReportService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPlanLimitService _planLimits;
    private readonly IReportFileStorage _storage;
    private readonly IAuditTrailService _audit;
    private readonly TenantRelationGuard _tenantGuard;

    public ReportService(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IPlanLimitService planLimits,
        IReportFileStorage storage,
        IAuditTrailService audit,
        TenantRelationGuard tenantGuard)
    {
        _db = db;
        _currentUser = currentUser;
        _planLimits = planLimits;
        _storage = storage;
        _audit = audit;
        _tenantGuard = tenantGuard;
    }

    public async Task<IReadOnlyList<ReportResponse>> GetAllAsync(CancellationToken ct = default) =>
        await Query()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => ToResponse(r))
            .ToListAsync(ct);

    public async Task<ReportResponse> GetByIdAsync(long id, CancellationToken ct = default) =>
        ToResponse(await FindAsync(id, ct));

    public async Task<ReportResponse> CreateAsync(
        CreateReportRequest request,
        Stream content,
        string contentType,
        string originalFileName,
        long length,
        CancellationToken ct = default)
    {
        if (_currentUser.UserId is not long userId)
            throw new UnauthorizedAccessException("Not authenticated");
        if (_currentUser.OrganizationId is not long orgId || orgId <= 0)
            throw new UnauthorizedAccessException("Organization context is required.");

        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Title is required.");

        if (!Enum.TryParse<ReportType>(request.ReportType?.Trim(), ignoreCase: true, out var reportType))
            throw new ArgumentException($"Invalid report type: {request.ReportType}");
        if (!Enum.TryParse<ReportFormat>(request.Format?.Trim(), ignoreCase: true, out var format))
            throw new ArgumentException($"Invalid report format: {request.Format}");

        // Buffer non-seekable uploads so magic-byte checks can rewind safely.
        Stream payload = content;
        MemoryStream? owned = null;
        if (!content.CanSeek)
        {
            owned = new MemoryStream(length > 0 && length <= int.MaxValue ? (int)length : 0);
            await content.CopyToAsync(owned, ct);
            owned.Position = 0;
            payload = owned;
            length = owned.Length;
        }

        try
        {
            _storage.Validate(payload, contentType, length, format.ToString());
            await _planLimits.EnsureStorageAvailableAsync(length, ct);
            await ValidateFiltersAsync(request, orgId, ct);

            if (payload.CanSeek)
                payload.Position = 0;

            var displayName = string.IsNullOrWhiteSpace(originalFileName)
                ? $"report-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}.{(format == ReportFormat.PDF ? "pdf" : "csv")}"
                : Path.GetFileName(originalFileName.Trim());

            var storageFileName = $"{Guid.NewGuid():N}-{Sanitize(displayName)}";
            string? storageKey = null;
            try
            {
                storageKey = await _storage.SaveAsync(orgId, storageFileName, payload, ct);

                var entity = new Report
                {
                    OrganizationId = orgId,
                    Title = request.Title.Trim(),
                    ReportType = reportType,
                    Format = format,
                    ProjectId = request.ProjectId,
                    DepartmentId = request.DepartmentId,
                    EmployeeId = request.EmployeeId,
                    DateFrom = request.DateFrom,
                    DateTo = request.DateTo,
                    FileName = displayName,
                    StorageKey = storageKey,
                    ContentType = contentType.Split(';', 2)[0].Trim(),
                    SizeBytes = length,
                    GeneratedById = userId,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _db.Add(entity);
                await _db.SaveChangesAsync(ct);

                var saved = await FindAsync(entity.Id, ct);
                await _audit.RecordCreateAsync(
                    AuditEntityType.REPORT,
                    saved.Id,
                    saved.Title,
                    AuditSnapshot.Serialize(new
                    {
                        saved.Title,
                        ReportType = saved.ReportType.ToString(),
                        Format = saved.Format.ToString(),
                        saved.ProjectId,
                        saved.FileName,
                        saved.SizeBytes
                    }),
                    $"Report created: {saved.Title} ({saved.Format})",
                    saved.ProjectId,
                    saved.Project?.Name,
                    ct);

                return ToResponse(saved);
            }
            catch
            {
                // Create wrote the file before the DB row — remove orphan on failure.
                if (storageKey != null)
                    _storage.Delete(orgId, storageKey);
                throw;
            }
        }
        finally
        {
            if (owned != null)
                await owned.DisposeAsync();
        }
    }

    public async Task<(Stream Stream, string ContentType, string FileName)> DownloadAsync(long id, CancellationToken ct = default)
    {
        if (_currentUser.OrganizationId is not long orgId || orgId <= 0)
            throw new UnauthorizedAccessException("Organization context is required.");

        var report = await FindAsync(id, ct);
        // Defense in depth: never stream a file outside the caller's tenant even if a key leaked.
        if (report.OrganizationId != orgId)
            throw new KeyNotFoundException("Report not found");

        var opened = await _storage.OpenAsync(report.OrganizationId, report.StorageKey, report.ContentType, ct)
            ?? throw new KeyNotFoundException("Report file not found on disk.");
        return (opened.Stream, opened.ContentType, report.FileName);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var report = await FindAsync(id, ct);
        var title = report.Title;
        var projectId = report.ProjectId;
        var projectName = report.Project?.Name;
        var orgId = report.OrganizationId;
        var key = report.StorageKey;
        var snapshot = AuditSnapshot.Serialize(new
        {
            report.Title,
            ReportType = report.ReportType.ToString(),
            Format = report.Format.ToString(),
            report.ProjectId,
            report.FileName,
            report.StorageKey,
            report.SizeBytes
        });

        _db.Remove(report);
        await _db.SaveChangesAsync(ct);
        _storage.Delete(orgId, key);

        await _audit.RecordDeleteAsync(
            AuditEntityType.REPORT,
            id,
            title,
            snapshot,
            $"Report deleted: {title}",
            projectId,
            projectName,
            ct);
    }

    private IQueryable<Report> Query() =>
        _db.Reports
            .Include(r => r.Project)
            .Include(r => r.Department)
            .Include(r => r.Employee)
            .Include(r => r.GeneratedBy);

    private async Task<Report> FindAsync(long id, CancellationToken ct) =>
        await Query().FirstOrDefaultAsync(r => r.Id == id, ct)
        ?? throw new KeyNotFoundException("Report not found");

    private async Task ValidateFiltersAsync(CreateReportRequest request, long organizationId, CancellationToken ct)
    {
        if (request.ProjectId is long projectId)
            await _tenantGuard.EnsureProjectAsync(projectId, organizationId, ct);

        await _tenantGuard.EnsureDepartmentAsync(request.DepartmentId, organizationId, ct);
        await _tenantGuard.EnsureUserAsync(request.EmployeeId, organizationId, ct);
    }

    private static ReportResponse ToResponse(Report r) => new(
        r.Id,
        r.Title,
        r.ReportType.ToString(),
        r.Format.ToString(),
        r.ProjectId,
        r.Project?.Name,
        r.DepartmentId,
        r.Department?.Name,
        r.EmployeeId,
        r.Employee?.Name,
        r.DateFrom,
        r.DateTo,
        r.FileName,
        r.ContentType,
        r.SizeBytes,
        $"/api/reports/{r.Id}/download",
        r.GeneratedById,
        r.GeneratedBy?.Name ?? "",
        r.CreatedAt);

    private static string Sanitize(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name;
    }
}
