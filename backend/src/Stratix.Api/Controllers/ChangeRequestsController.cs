using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.ChangeRequests;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,EMPLOYEE,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class ChangeRequestsController : ControllerBase
{
    private readonly IChangeRequestService _service;

    public ChangeRequestsController(IChangeRequestService service) => _service = service;

    [HttpGet("change-requests")]
    public async Task<IReadOnlyList<ChangeRequestResponse>> GetAll([FromQuery] long? projectId, CancellationToken ct) =>
        await _service.GetAllAsync(projectId, ct);

    [HttpGet("change-requests/{id:long}")]
    public async Task<ChangeRequestResponse> Get(long id, CancellationToken ct) => await _service.GetByIdAsync(id, ct);

    [HttpPost("change-requests")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,TEAM_LEADER,EMPLOYEE")]
    public async Task<ActionResult<ChangeRequestResponse>> Create([FromBody] CreateChangeRequestRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("change-requests/{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER")]
    public async Task<ChangeRequestResponse> Update(long id, [FromBody] UpdateChangeRequestRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);

    [HttpDelete("change-requests/{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
