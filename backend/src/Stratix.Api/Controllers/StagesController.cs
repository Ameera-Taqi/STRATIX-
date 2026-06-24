using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Stages;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN,PROJECT_MANAGER,EMPLOYEE,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class StagesController : ControllerBase
{
    private readonly IStageService _stages;

    public StagesController(IStageService stages) => _stages = stages;

    [HttpGet("api/projects/{projectId:long}/stages")]
    public async Task<IReadOnlyList<StageResponse>> GetByProject(long projectId, CancellationToken ct) =>
        await _stages.GetByProjectAsync(projectId, ct);

    [HttpPost("api/projects/{projectId:long}/stages")]
    [Authorize(Roles = "ADMIN,PROJECT_MANAGER")]
    public async Task<ActionResult<StageResponse>> Create(long projectId, [FromBody] CreateStageRequest request, CancellationToken ct)
    {
        var stage = await _stages.CreateAsync(projectId, request, ct);
        return Created($"api/stages/{stage.Id}", stage);
    }

    [HttpPut("api/stages/{id:long}")]
    [Authorize(Roles = "ADMIN,PROJECT_MANAGER")]
    public async Task<StageResponse> Update(long id, [FromBody] UpdateStageRequest request, CancellationToken ct) =>
        await _stages.UpdateAsync(id, request, ct);

    [HttpPatch("api/stages/{id:long}/complete")]
    [Authorize(Roles = "ADMIN,PROJECT_MANAGER")]
    public async Task<StageResponse> Complete(long id, CancellationToken ct) =>
        await _stages.CompleteAsync(id, ct);

    [HttpDelete("api/stages/{id:long}")]
    [Authorize(Roles = "ADMIN,PROJECT_MANAGER")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _stages.DeleteAsync(id, ct);
        return NoContent();
    }
}
