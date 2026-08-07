namespace Stratix.Application.Common;

/// <summary>
/// Magic-byte / header checks for important upload types.
/// Content-Type alone is not trusted — clients can spoof it.
/// </summary>
public static class FileSignatureValidator
{
    public static void EnsureMatches(Stream content, string contentType, string? formatHint = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!content.CanSeek)
            throw new ArgumentException("Upload stream must be seekable for signature validation.");

        var normalized = (contentType ?? "").Split(';', 2)[0].Trim().ToLowerInvariant();
        var hint = (formatHint ?? "").Trim().ToUpperInvariant();

        var header = new byte[64];
        var pos = content.Position;
        var read = content.Read(header, 0, header.Length);
        content.Position = pos;
        if (read <= 0)
            throw new ArgumentException("File is empty.");

        if (normalized is "application/pdf" || hint == "PDF")
        {
            EnsurePdf(header.AsSpan(0, read));
            return;
        }

        if (normalized is "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            || (hint == "EXCEL" && normalized.Contains("openxml", StringComparison.Ordinal)))
        {
            EnsureZipContainer(header.AsSpan(0, read), "Excel (.xlsx)");
            return;
        }

        if (normalized is "application/vnd.ms-excel" || hint == "EXCEL")
        {
            // Legacy .xls (OLE) or mislabeled CSV/text exports — allow OLE or ZIP (xlsx mistyped).
            if (LooksLikeOle(header.AsSpan(0, read)) || LooksLikeZip(header.AsSpan(0, read)))
                return;
            throw new ArgumentException("Excel file signature is invalid.");
        }

        if (normalized is "image/png")
        {
            EnsurePng(header.AsSpan(0, read));
            return;
        }

        if (normalized is "image/jpeg" or "image/jpg")
        {
            EnsureJpeg(header.AsSpan(0, read));
            return;
        }

        if (normalized is "image/webp")
        {
            EnsureWebp(header.AsSpan(0, read));
            return;
        }

        if (normalized is "image/gif")
        {
            EnsureGif(header.AsSpan(0, read));
            return;
        }

        if (normalized is "image/svg+xml")
        {
            EnsureSvg(content);
            return;
        }

        // text/csv, text/plain, text/html — reject known binary executables / archives pretending to be text.
        if (normalized.StartsWith("text/", StringComparison.Ordinal) || normalized is "application/csv")
        {
            if (LooksLikeZip(header.AsSpan(0, read)) || LooksLikeOle(header.AsSpan(0, read)) || LooksLikePdf(header.AsSpan(0, read)))
                throw new ArgumentException("Text/CSV content has a binary file signature.");
            if (header.AsSpan(0, read).IndexOf((byte)0) >= 0)
                throw new ArgumentException("Text/CSV content contains binary null bytes.");
            return;
        }
    }

    private static void EnsurePdf(ReadOnlySpan<byte> header)
    {
        if (!LooksLikePdf(header))
            throw new ArgumentException("PDF file signature is invalid (expected %PDF).");
    }

    private static bool LooksLikePdf(ReadOnlySpan<byte> header) =>
        header.Length >= 4
        && header[0] == (byte)'%'
        && header[1] == (byte)'P'
        && header[2] == (byte)'D'
        && header[3] == (byte)'F';

    private static void EnsureZipContainer(ReadOnlySpan<byte> header, string label)
    {
        if (!LooksLikeZip(header))
            throw new ArgumentException($"{label} signature is invalid (expected ZIP/OOXML).");
    }

    private static bool LooksLikeZip(ReadOnlySpan<byte> header) =>
        header.Length >= 4
        && header[0] == 0x50
        && header[1] == 0x4B
        && (header[2] == 0x03 || header[2] == 0x05 || header[2] == 0x07)
        && (header[3] == 0x04 || header[3] == 0x06 || header[3] == 0x08);

    private static bool LooksLikeOle(ReadOnlySpan<byte> header) =>
        header.Length >= 8
        && header[0] == 0xD0
        && header[1] == 0xCF
        && header[2] == 0x11
        && header[3] == 0xE0
        && header[4] == 0xA1
        && header[5] == 0xB1
        && header[6] == 0x1A
        && header[7] == 0xE1;

    private static void EnsurePng(ReadOnlySpan<byte> header)
    {
        ReadOnlySpan<byte> sig = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        if (header.Length < sig.Length || !header[..sig.Length].SequenceEqual(sig))
            throw new ArgumentException("PNG file signature is invalid.");
    }

    private static void EnsureJpeg(ReadOnlySpan<byte> header)
    {
        if (header.Length < 3 || header[0] != 0xFF || header[1] != 0xD8 || header[2] != 0xFF)
            throw new ArgumentException("JPEG file signature is invalid.");
    }

    private static void EnsureWebp(ReadOnlySpan<byte> header)
    {
        if (header.Length < 12
            || header[0] != (byte)'R' || header[1] != (byte)'I' || header[2] != (byte)'F' || header[3] != (byte)'F'
            || header[8] != (byte)'W' || header[9] != (byte)'E' || header[10] != (byte)'B' || header[11] != (byte)'P')
            throw new ArgumentException("WebP file signature is invalid.");
    }

    private static void EnsureGif(ReadOnlySpan<byte> header)
    {
        if (header.Length < 6) throw new ArgumentException("GIF file signature is invalid.");
        var s = System.Text.Encoding.ASCII.GetString(header[..6]);
        if (s is not ("GIF87a" or "GIF89a"))
            throw new ArgumentException("GIF file signature is invalid.");
    }

    private static void EnsureSvg(Stream content)
    {
        var pos = content.Position;
        using var reader = new StreamReader(content, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var sample = new char[4096];
        var n = reader.Read(sample, 0, sample.Length);
        content.Position = pos;
        var text = new string(sample, 0, n);
        var trimmed = text.TrimStart('\uFEFF', ' ', '\t', '\r', '\n');
        if (!trimmed.StartsWith('<') || trimmed.IndexOf("svg", StringComparison.OrdinalIgnoreCase) < 0)
            throw new ArgumentException("SVG file signature is invalid.");
        if (trimmed.Contains("<script", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("javascript:", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("SVG content is not allowed to include scripts.");
    }
}
