using Stratix.Application.Common;

namespace Stratix.Api.Tests;

public class FileSignatureValidatorTests
{
    [Fact]
    public void Accepts_valid_pdf_header()
    {
        using var stream = new MemoryStream("%PDF-1.7 fake"u8.ToArray());
        FileSignatureValidator.EnsureMatches(stream, "application/pdf", "PDF");
    }

    [Fact]
    public void Rejects_pdf_content_type_with_wrong_bytes()
    {
        using var stream = new MemoryStream("not-a-pdf"u8.ToArray());
        var ex = Assert.Throws<ArgumentException>(() =>
            FileSignatureValidator.EnsureMatches(stream, "application/pdf", "PDF"));
        Assert.Contains("PDF", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Accepts_png_signature()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 };
        using var stream = new MemoryStream(bytes);
        FileSignatureValidator.EnsureMatches(stream, "image/png");
    }

    [Fact]
    public void Rejects_xlsx_content_type_without_zip_signature()
    {
        using var stream = new MemoryStream("MZ executable pretends excel"u8.ToArray());
        Assert.Throws<ArgumentException>(() =>
            FileSignatureValidator.EnsureMatches(
                stream,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "EXCEL"));
    }

    [Fact]
    public void Rejects_svg_with_script()
    {
        using var stream = new MemoryStream("<svg><script>alert(1)</script></svg>"u8.ToArray());
        Assert.Throws<ArgumentException>(() =>
            FileSignatureValidator.EnsureMatches(stream, "image/svg+xml"));
    }
}
