using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Branding;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/organization")]
[Authorize]
public class OrganizationBrandingController : ControllerBase
{
    private readonly IOrganizationBrandingService _branding;

    public OrganizationBrandingController(IOrganizationBrandingService branding) => _branding = branding;

    [HttpGet("branding")]
    public async Task<OrganizationBrandingResponse> GetBranding(CancellationToken ct) =>
        await _branding.GetAsync(ct);

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo(CancellationToken ct)
    {
        var logo = await _branding.OpenLogoAsync(ct);
        if (logo == null) return NotFound();
        return File(logo.Value.Stream, logo.Value.ContentType);
    }

    [HttpPost("logo")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    [RequestSizeLimit(2_500_000)]
    public async Task<ActionResult<OrganizationLogoUploadResult>> UploadLogo(IFormFile file, CancellationToken ct)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Logo file is required." });

            await using var stream = file.OpenReadStream();
            return Ok(await _branding.UploadAsync(stream, file.ContentType, file.FileName, file.Length, ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("logo")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<IActionResult> ClearLogo(CancellationToken ct)
    {
        try
        {
            await _branding.ClearAsync(ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
