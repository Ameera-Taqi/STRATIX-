namespace Stratix.Application.DTOs.Reports;

public record ReportResponse(
    long Id,
    string Title,
    string ReportType,
    string Format,
    long? ProjectId,
    string? ProjectName,
    long? DepartmentId,
    string? DepartmentName,
    long? EmployeeId,
    string? EmployeeName,
    DateOnly? DateFrom,
    DateOnly? DateTo,
    string FileName,
    string? ContentType,
    long SizeBytes,
    string DownloadUrl,
    long GeneratedById,
    string GeneratedByName,
    DateTimeOffset CreatedAt);

public class CreateReportRequest
{
    public string Title { get; set; } = string.Empty;
    public string ReportType { get; set; } = "CUSTOM";
    public string Format { get; set; } = "PDF";
    public long? ProjectId { get; set; }
    public long? DepartmentId { get; set; }
    public long? EmployeeId { get; set; }
    public DateOnly? DateFrom { get; set; }
    public DateOnly? DateTo { get; set; }
}
