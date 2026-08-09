using System.Globalization;
using System.Text;

namespace Stratix.Application.Services.Reports;

/// <summary>
/// Minimal multi-page PDF writer (Helvetica) for tabular reports — produces real %PDF bytes.
/// </summary>
internal static class SimplePdfWriter
{
    public static byte[] Write(ReportDocumentModel model)
    {
        var pages = BuildPages(model);
        var objects = new List<byte[]>();
        // object 1 = catalog, 2 = pages, then page+content pairs, then font
        objects.Add([]); // placeholder index 0 unused
        objects.Add(Encoding.ASCII.GetBytes("1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n"));
        objects.Add([]); // pages placeholder

        var pageObjectNumbers = new List<int>();
        var fontObjectNumber = 3 + pages.Count * 2;

        for (var i = 0; i < pages.Count; i++)
        {
            var pageObj = 3 + i * 2;
            var contentObj = pageObj + 1;
            pageObjectNumbers.Add(pageObj);

            var contentStream = BuildContentStream(pages[i]);
            var pageDict =
                $"{pageObj} 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] " +
                $"/Contents {contentObj} 0 R /Resources << /Font << /F1 {fontObjectNumber} 0 R >> >> >>endobj\n";
            objects.Add(Encoding.ASCII.GetBytes(pageDict));

            var contentObjBytes = Encoding.ASCII.GetBytes(
                $"{contentObj} 0 obj<< /Length {contentStream.Length} >>stream\n");
            var end = Encoding.ASCII.GetBytes("\nendstream\nendobj\n");
            var combined = new byte[contentObjBytes.Length + contentStream.Length + end.Length];
            Buffer.BlockCopy(contentObjBytes, 0, combined, 0, contentObjBytes.Length);
            Buffer.BlockCopy(contentStream, 0, combined, contentObjBytes.Length, contentStream.Length);
            Buffer.BlockCopy(end, 0, combined, contentObjBytes.Length + contentStream.Length, end.Length);
            objects.Add(combined);
        }

        objects.Add(Encoding.ASCII.GetBytes(
            $"{fontObjectNumber} 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n"));

        var kids = string.Join(" ", pageObjectNumbers.Select(n => $"{n} 0 R"));
        objects[2] = Encoding.ASCII.GetBytes(
            $"2 0 obj<< /Type /Pages /Kids [ {kids} ] /Count {pages.Count} >>endobj\n");

        using var ms = new MemoryStream();
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n");
        ms.Write(header);

        var offsets = new List<long> { 0 };
        for (var i = 1; i < objects.Count; i++)
        {
            offsets.Add(ms.Position);
            ms.Write(objects[i]);
        }

        var xrefPos = ms.Position;
        var xref = new StringBuilder();
        xref.Append("xref\n");
        xref.Append(CultureInfo.InvariantCulture, $"0 {objects.Count}\n");
        xref.Append("0000000000 65535 f \n");
        for (var i = 1; i < objects.Count; i++)
            xref.Append(CultureInfo.InvariantCulture, $"{offsets[i]:D10} 00000 n \n");
        xref.Append("trailer<< /Size ");
        xref.Append(objects.Count);
        xref.Append(" /Root 1 0 R >>\nstartxref\n");
        xref.Append(xrefPos);
        xref.Append("\n%%EOF\n");
        ms.Write(Encoding.ASCII.GetBytes(xref.ToString()));
        return ms.ToArray();
    }

    private static List<List<string>> BuildPages(ReportDocumentModel model)
    {
        const int maxLines = 48;
        var allLines = new List<string>
        {
            "STRATIX",
            model.Title,
            $"Type: {model.ReportType}  |  Generated: {model.GeneratedAt:yyyy-MM-dd HH:mm} UTC",
            $"Project: {model.ProjectName ?? "All"}  |  Period: {FormatPeriod(model.DateFrom, model.DateTo)}",
            ""
        };

        foreach (var section in model.Sections)
        {
            allLines.Add(section.Heading);
            foreach (var line in section.SummaryLines)
                allLines.Add("  " + line);
            if (section.Columns.Count > 0)
                allLines.Add(string.Join(" | ", section.Columns));
            foreach (var row in section.Rows)
                allLines.Add(string.Join(" | ", row.Select(Truncate)));
            allLines.Add("");
        }

        if (allLines.Count <= 5)
            allLines.Add("(No data for the selected filters.)");

        var pages = new List<List<string>>();
        for (var i = 0; i < allLines.Count; i += maxLines)
            pages.Add(allLines.GetRange(i, Math.Min(maxLines, allLines.Count - i)));
        return pages;
    }

    private static byte[] BuildContentStream(IReadOnlyList<string> lines)
    {
        var sb = new StringBuilder();
        sb.Append("BT\n/F1 10 Tf\n14 TL\n50 760 Td\n");
        for (var i = 0; i < lines.Count; i++)
        {
            if (i > 0) sb.Append("T*\n");
            var size = i switch
            {
                0 => 16,
                1 => 13,
                _ => 9
            };
            if (i <= 1)
                sb.Append(CultureInfo.InvariantCulture, $"/F1 {size} Tf\n");
            else if (i == 2)
                sb.Append("/F1 9 Tf\n");
            sb.Append('(');
            sb.Append(EscapePdf(lines[i]));
            sb.Append(") Tj\n");
        }
        sb.Append("ET");
        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private static string FormatPeriod(DateOnly? from, DateOnly? to)
    {
        if (from is null && to is null) return "All time";
        return $"{from?.ToString("yyyy-MM-dd") ?? "…"} → {to?.ToString("yyyy-MM-dd") ?? "…"}";
    }

    private static string Truncate(string value) =>
        value.Length <= 40 ? value : value[..37] + "...";

    private static string EscapePdf(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
        {
            if (ch is '(' or ')' or '\\')
                sb.Append('\\').Append(ch);
            else if (ch < 32 || ch > 126)
                sb.Append('?');
            else
                sb.Append(ch);
        }
        return sb.ToString();
    }
}
