using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Cms;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/cms")]
[Authorize]
public class CmsController : ControllerBase
{
    private readonly IPlatformCmsService _cms;

    public CmsController(IPlatformCmsService cms) => _cms = cms;

    /// <summary>Effective company-admin module permissions (any authenticated user).</summary>
    [HttpGet("company-admin-permissions")]
    public async Task<IReadOnlyList<CompanyAdminModulePermissionResponse>> Get(CancellationToken ct) =>
        await _cms.GetAllAsync(ct);

    /// <summary>Update company-admin module permissions — Super Admin only.</summary>
    [HttpPut("company-admin-permissions")]
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<ActionResult<IReadOnlyList<CompanyAdminModulePermissionResponse>>> Update(
        [FromBody] UpdateCompanyAdminPermissionsRequest request,
        CancellationToken ct)
    {
        try
        {
            return Ok(await _cms.UpdateAsync(request, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
