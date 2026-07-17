using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Roles;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize]
public class RolesController : ControllerBase
{
    private readonly IOrganizationRoleService _roles;

    public RolesController(IOrganizationRoleService roles) => _roles = roles;

    [HttpGet]
    public async Task<IReadOnlyList<OrganizationRoleResponse>> GetAll(CancellationToken ct) =>
        await _roles.GetAllAsync(ct);

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<ActionResult<OrganizationRoleResponse>> Create(
        [FromBody] CreateOrganizationRoleRequest request,
        CancellationToken ct)
    {
        try
        {
            var role = await _roles.CreateAsync(request, ct);
            return CreatedAtAction(nameof(GetAll), role);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<ActionResult<OrganizationRoleResponse>> Update(
        long id,
        [FromBody] UpdateOrganizationRoleRequest request,
        CancellationToken ct)
    {
        try
        {
            return await _roles.UpdateAsync(id, request, ct);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        try
        {
            await _roles.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
