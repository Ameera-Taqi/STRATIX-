using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Tasks;
using Stratix.Application.Interfaces;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Policy = AuthPolicies.AllTenantUsers)]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks) => _tasks = tasks;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IReadOnlyList<TaskResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] long? projectId, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        if (pageSize is > 0)
        {
            var paged = await _tasks.GetPagedAsync(projectId, page ?? 1, pageSize.Value, ct);
            PagingHeaders.Apply(Response, paged);
            return Ok(paged.ToResponse());
        }

        return Ok(await _tasks.GetAllAsync(projectId, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<TaskResponse> Get(long id, CancellationToken ct) => await _tasks.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = AuthPolicies.TeamLeaders)]
    public async Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _tasks.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthPolicies.TeamLeaders)]
    public async Task<TaskResponse> Update(long id, [FromBody] UpdateTaskRequest request, CancellationToken ct) =>
        await _tasks.UpdateAsync(id, request, ct);

    [HttpPatch("{id:long}/status")]
    [Authorize(Policy = AuthPolicies.TaskContributors)]
    public async Task<TaskResponse> UpdateStatus(long id, [FromBody] UpdateTaskStatusRequest request, CancellationToken ct) =>
        await _tasks.UpdateStatusAsync(id, request, ct);

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthPolicies.TeamLeaders)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _tasks.DeleteAsync(id, ct);
        return NoContent();
    }
}
