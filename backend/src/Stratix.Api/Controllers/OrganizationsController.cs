using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Organizations;
using Stratix.Application.Interfaces;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly IOrganizationService _organizations;

    public OrganizationsController(IOrganizationService organizations) => _organizations = organizations;

    // The caller's own organization.
    [HttpGet("organizations/current")]
    public async Task<ActionResult<OrganizationResponse>> Current(CancellationToken ct)
    {
        var org = await _organizations.GetCurrentAsync(ct);
        return org == null ? NotFound() : Ok(org);
    }

    // Platform-wide list — super-admins only.
    [HttpGet("organizations")]
    [Authorize(Policy = AuthPolicies.SuperAdmin)]
    public async Task<IReadOnlyList<OrganizationResponse>> All(CancellationToken ct) =>
        await _organizations.GetAllAsync(ct);

    // Create company + org admin — super-admins only.
    [HttpPost("organizations")]
    [Authorize(Policy = AuthPolicies.SuperAdmin)]
    public async Task<ActionResult<OrganizationResponse>> Create([FromBody] CreateOrganizationRequest request, CancellationToken ct)
    {
        try
        {
            var org = await _organizations.CreateAsync(request, ct);
            return CreatedAtAction(nameof(All), org);
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

    // Update status / plan — super-admins only.
    [HttpPatch("organizations/{id:long}")]
    [Authorize(Policy = AuthPolicies.SuperAdmin)]
    public async Task<ActionResult<OrganizationResponse>> Update(long id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
    {
        try
        {
            var org = await _organizations.UpdateAsync(id, request, ct);
            return org == null ? NotFound() : Ok(org);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // Cancel (soft-delete) an organization — super-admins only.
    [HttpDelete("organizations/{id:long}")]
    [Authorize(Policy = AuthPolicies.SuperAdmin)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _organizations.DeleteAsync(id, ct);
        return NoContent();
    }

    // The caller's current subscription (plan, status, trial end).
    [HttpGet("organizations/current/subscription")]
    public async Task<ActionResult<SubscriptionResponse>> Subscription(CancellationToken ct)
    {
        var sub = await _organizations.GetCurrentSubscriptionAsync(ct);
        return sub == null ? NotFound() : Ok(sub);
    }

    // Available subscription tiers.
    [HttpGet("plans")]
    public async Task<IReadOnlyList<PlanResponse>> Plans(CancellationToken ct) =>
        await _organizations.GetPlansAsync(ct);
}
