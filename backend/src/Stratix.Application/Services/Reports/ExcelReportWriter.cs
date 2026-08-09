using ClosedXML.Excel;

namespace Stratix.Application.Services.Reports;

internal static class ExcelReportWriter
{
    public static byte[] Write(ReportDocumentModel model)
    {
        using var workbook = new XLWorkbook();
        var cover = workbook.Worksheets.Add("Summary");
        cover.Cell(1, 1).Value = "STRATIX";
        cover.Cell(2, 1).Value = model.Title;
        cover.Cell(3, 1).Value = $"Type: {model.ReportType}";
        cover.Cell(4, 1).Value = $"Generated (UTC): {model.GeneratedAt:yyyy-MM-dd HH:mm}";
        cover.Cell(5, 1).Value = $"Project: {model.ProjectName ?? "All"}";
        cover.Cell(6, 1).Value =
            $"Period: {model.DateFrom?.ToString("yyyy-MM-dd") ?? "…"} → {model.DateTo?.ToString("yyyy-MM-dd") ?? "…"}";
        cover.Columns().AdjustToContents();

        var sheetIndex = 1;
        foreach (var section in model.Sections)
        {
            var name = SanitizeSheetName(section.Heading, sheetIndex++);
            var ws = workbook.Worksheets.Add(name);
            var row = 1;
            foreach (var line in section.SummaryLines)
            {
                ws.Cell(row++, 1).Value = line;
            }

            if (section.Columns.Count > 0)
            {
                for (var c = 0; c < section.Columns.Count; c++)
                    ws.Cell(row, c + 1).Value = section.Columns[c];
                ws.Row(row).Style.Font.Bold = true;
                row++;

                foreach (var dataRow in section.Rows)
                {
                    for (var c = 0; c < dataRow.Count; c++)
                        ws.Cell(row, c + 1).Value = dataRow[c];
                    row++;
                }
            }

            if (section.Columns.Count == 0 && section.SummaryLines.Count == 0 && section.Rows.Count == 0)
                ws.Cell(1, 1).Value = "No data";

            ws.Columns().AdjustToContents();
        }

        if (model.Sections.Count == 0)
        {
            var empty = workbook.Worksheets.Add("Data");
            empty.Cell(1, 1).Value = "No data for the selected filters.";
        }

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    private static string SanitizeSheetName(string heading, int index)
    {
        var name = string.IsNullOrWhiteSpace(heading) ? $"Sheet{index}" : heading.Trim();
        foreach (var c in Path.GetInvalidFileNameChars().Concat([':', '\\', '/', '?', '*', '[', ']']))
            name = name.Replace(c, '_');
        if (name.Length > 28)
            name = name[..28];
        return $"{index}_{name}";
    }
}
