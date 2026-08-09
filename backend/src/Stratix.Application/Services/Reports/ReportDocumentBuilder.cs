using Stratix.Application.Interfaces;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services.Reports;

public sealed class ReportDocumentBuilder : IReportDocumentBuilder
{
    public GeneratedReportFile Build(ReportDocumentModel model, ReportFormat format)
    {
        var slug = Slug(model.Title);
        var stamp = model.GeneratedAt.ToString("yyyyMMddHHmmss");

        if (format == ReportFormat.EXCEL)
        {
            var bytes = ExcelReportWriter.Write(model);
            return new GeneratedReportFile
            {
                Content = bytes,
                ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                FileName = $"{slug}-{stamp}.xlsx"
            };
        }

        var pdf = SimplePdfWriter.Write(model);
        return new GeneratedReportFile
        {
            Content = pdf,
            ContentType = "application/pdf",
            FileName = $"{slug}-{stamp}.pdf"
        };
    }

    private static string Slug(string title)
    {
        var chars = title.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        slug = slug.Trim('-');
        if (slug.Length > 40) slug = slug[..40].TrimEnd('-');
        return string.IsNullOrEmpty(slug) ? "report" : slug;
    }
}
