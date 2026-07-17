using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Organizations;
using Stratix.Application.Interfaces;

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
    [Authorize(Roles = "SUPER_ADMIN")]
    public async Task<IReadOnlyList<OrganizationResponse>> All(CancellationToken ct) =>
        await _organizations.GetAllAsync(ct);

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
