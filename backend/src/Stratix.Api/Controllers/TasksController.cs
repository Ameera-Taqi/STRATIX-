using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Tasks;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/tasks")]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,EMPLOYEE,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _tasks;

    public TasksController(ITaskService tasks) => _tasks = tasks;

    [HttpGet]
    public async Task<IReadOnlyList<TaskResponse>> GetAll([FromQuery] long? projectId, [FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        if (pageSize is > 0)
        {
            var paged = await _tasks.GetPagedAsync(projectId, page ?? 1, pageSize.Value, ct);
            PagingHeaders.Apply(Response, paged.Total, paged.Page, paged.PageSize, paged.TotalPages);
            return paged.Items;
        }
        return await _tasks.GetAllAsync(projectId, ct);
    }

    [HttpGet("{id:long}")]
    public async Task<TaskResponse> Get(long id, CancellationToken ct) => await _tasks.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,TEAM_LEADER")]
    public async Task<ActionResult<TaskResponse>> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        var task = await _tasks.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,TEAM_LEADER")]
    public async Task<TaskResponse> Update(long id, [FromBody] UpdateTaskRequest request, CancellationToken ct) =>
        await _tasks.UpdateAsync(id, request, ct);

    [HttpPatch("{id:long}/status")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,EMPLOYEE,TEAM_LEADER")]
    public async Task<TaskResponse> UpdateStatus(long id, [FromBody] UpdateTaskStatusRequest request, CancellationToken ct) =>
        await _tasks.UpdateStatusAsync(id, request, ct);

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,TEAM_LEADER")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _tasks.DeleteAsync(id, ct);
        return NoContent();
    }
}
