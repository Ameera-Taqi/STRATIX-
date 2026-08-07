using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Branding;
using Stratix.Application.DTOs.Onboarding;
using Stratix.Application.Interfaces;
using Stratix.Application.Services;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/organization")]
[Authorize]
public class OrganizationBrandingController : ControllerBase
{
    private readonly IOrganizationBrandingService _branding;
    private readonly IOrganizationOnboardingService _onboarding;

    public OrganizationBrandingController(
        IOrganizationBrandingService branding,
        IOrganizationOnboardingService onboarding)
    {
        _branding = branding;
        _onboarding = onboarding;
    }

    [HttpGet("branding")]
    public async Task<OrganizationBrandingResponse> GetBranding(CancellationToken ct) =>
        await _branding.GetAsync(ct);

    [HttpGet("onboarding")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<OrganizationOnboardingStatusResponse> GetOnboarding(CancellationToken ct) =>
        await _onboarding.GetStatusAsync(ct);

    [HttpPut("profile")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<ActionResult<OrganizationOnboardingStatusResponse>> UpdateProfile(
        [FromBody] UpdateOrganizationProfileRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _onboarding.UpdateProfileAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("onboarding/complete")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<OrganizationOnboardingStatusResponse> CompleteOnboarding(CancellationToken ct) =>
        await _onboarding.CompleteAsync(ct);

    [HttpGet("logo")]
    public async Task<IActionResult> GetLogo(CancellationToken ct)
    {
        var logo = await _branding.OpenLogoAsync(ct);
        if (logo == null) return NotFound();
        return File(logo.Value.Stream, logo.Value.ContentType);
    }

    [HttpPost("logo")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
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
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
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
