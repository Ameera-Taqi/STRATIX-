namespace Stratix.Application.Services.Reports;

public sealed class ReportDocumentModel
{
    public required string Title { get; init; }
    public required string ReportType { get; init; }
    public required string Format { get; init; }
    public string? ProjectName { get; init; }
    public DateOnly? DateFrom { get; init; }
    public DateOnly? DateTo { get; init; }
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<ReportSection> Sections { get; init; } = Array.Empty<ReportSection>();
}

public sealed class ReportSection
{
    public required string Heading { get; init; }
    public IReadOnlyList<string> Columns { get; init; } = Array.Empty<string>();
    public IReadOnlyList<IReadOnlyList<string>> Rows { get; init; } = Array.Empty<IReadOnlyList<string>>();
    public IReadOnlyList<string> SummaryLines { get; init; } = Array.Empty<string>();
}

public sealed class GeneratedReportFile
{
    public required byte[] Content { get; init; }
    public required string ContentType { get; init; }
    public required string FileName { get; init; }
}
