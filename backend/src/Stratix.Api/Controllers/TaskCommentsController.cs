using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.TaskComments;
using Stratix.Application.Interfaces;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Policy = AuthPolicies.AllTenantUsers)]
public class TaskCommentsController : ControllerBase
{
    private readonly ITaskCommentService _comments;

    public TaskCommentsController(ITaskCommentService comments) => _comments = comments;

    [HttpGet("tasks/{taskId:long}/comments")]
    public async Task<IReadOnlyList<TaskCommentResponse>> ByTask(long taskId, CancellationToken ct) =>
        await _comments.GetByTaskAsync(taskId, ct);

    [HttpPost("task-comments")]
    public async Task<ActionResult<TaskCommentResponse>> Create([FromBody] CreateTaskCommentRequest request, CancellationToken ct)
    {
        var comment = await _comments.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, comment);
    }

    [HttpDelete("task-comments/{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _comments.DeleteAsync(id, ct);
        return NoContent();
    }
}
