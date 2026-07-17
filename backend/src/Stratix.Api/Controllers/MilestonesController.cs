using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Milestones;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,EMPLOYEE,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class MilestonesController : ControllerBase
{
    private readonly IMilestoneService _milestones;

    public MilestonesController(IMilestoneService milestones) => _milestones = milestones;

    [HttpGet("milestones")]
    public async Task<IReadOnlyList<MilestoneResponse>> GetAll(CancellationToken ct) => await _milestones.GetAllAsync(ct);

    [HttpGet("projects/{projectId:long}/milestones")]
    public async Task<IReadOnlyList<MilestoneResponse>> ByProject(long projectId, CancellationToken ct) =>
        await _milestones.GetByProjectAsync(projectId, ct);

    [HttpGet("milestones/{id:long}")]
    public async Task<MilestoneResponse> Get(long id, CancellationToken ct) => await _milestones.GetByIdAsync(id, ct);

    [HttpPost("milestones")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER")]
    public async Task<ActionResult<MilestoneResponse>> Create([FromBody] CreateMilestoneRequest request, CancellationToken ct)
    {
        var milestone = await _milestones.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = milestone.Id }, milestone);
    }

    [HttpPut("milestones/{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER")]
    public async Task<MilestoneResponse> Update(long id, [FromBody] UpdateMilestoneRequest request, CancellationToken ct) =>
        await _milestones.UpdateAsync(id, request, ct);

    [HttpDelete("milestones/{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _milestones.DeleteAsync(id, ct);
        return NoContent();
    }
}
