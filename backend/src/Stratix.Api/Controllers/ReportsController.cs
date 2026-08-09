using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Api.Auth;
using Stratix.Application.DTOs.Reports;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = AuthPolicies.LeadersAndExecutives)]
public class ReportsController : ControllerBase
{
    private readonly IReportService _service;

    public ReportsController(IReportService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<ReportResponse>> GetAll(CancellationToken ct) =>
        await _service.GetAllAsync(ct);

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ReportResponse>> Get(long id, CancellationToken ct) =>
        Ok(await _service.GetByIdAsync(id, ct));

    /// <summary>
    /// Generate a report on the server from authoritative data, store it, and return metadata + download URL.
    /// </summary>
    [HttpPost("generate")]
    [Authorize(Policy = AuthPolicies.LeadersAndExecutives)]
    public async Task<ActionResult<ReportResponse>> Generate(
        [FromBody] GenerateReportRequest request,
        CancellationToken ct)
    {
        var created = await _service.GenerateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>
    /// Legacy: upload a pre-generated report file (PDF / Excel) with metadata.
    /// Prefer <see cref="Generate"/> for new clients.
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(26 * 1024 * 1024)]
    [Authorize(Policy = AuthPolicies.LeadersAndExecutives)]
    public async Task<ActionResult<ReportResponse>> Create(
        [FromForm] CreateReportRequest request,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Report file is required." });

        await using var stream = file.OpenReadStream();
        var created = await _service.CreateAsync(
            request,
            stream,
            file.ContentType ?? "application/octet-stream",
            file.FileName,
            file.Length,
            ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpGet("{id:long}/download")]
    public async Task<IActionResult> Download(long id, CancellationToken ct)
    {
        var (stream, contentType, fileName) = await _service.DownloadAsync(id, ct);
        return File(stream, contentType, fileName);
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
