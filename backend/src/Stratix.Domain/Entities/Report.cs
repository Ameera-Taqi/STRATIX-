using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

/// <summary>
/// A generated analytics export stored for a tenant (PDF / Excel).
/// Binary content lives under the report file storage root; this row is the metadata.
/// </summary>
public class Report : ITenantScoped, ISoftDeletable, IHasCreatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public ReportType ReportType { get; set; } = ReportType.CUSTOM;
    public ReportFormat Format { get; set; } = ReportFormat.PDF;
    public long? ProjectId { get; set; }
    public Project? Project { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public long? EmployeeId { get; set; }
    public User? Employee { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
    /// <summary>Original / display file name (e.g. executive-report.pdf).</summary>
    public string FileName { get; set; } = string.Empty;
    /// <summary>Relative storage key under the reports root (org-scoped path).</summary>
    public string StorageKey { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public long GeneratedById { get; set; }
    public User? GeneratedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
