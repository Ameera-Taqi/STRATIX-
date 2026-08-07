using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.Interfaces;
using Stratix.Application.Services;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Policy = AuthPolicies.AllTenantUsers)]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projects;
    private readonly IProgressRecalculationService _progress;

    public ProjectsController(IProjectService projects, IProgressRecalculationService progress)
    {
        _projects = projects;
        _progress = progress;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        if (pageSize is > 0)
        {
            var paged = await _projects.GetPagedAsync(page ?? 1, pageSize.Value, ct);
            PagingHeaders.Apply(Response, paged);
            return Ok(paged.ToResponse());
        }

        return Ok(await _projects.GetAllAsync(ct));
    }

    [HttpGet("{id:long}")]
    public async Task<ProjectResponse> Get(long id, CancellationToken ct) => await _projects.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<ActionResult<ProjectResponse>> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var project = await _projects.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }

    [HttpPut("{id:long}")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<ProjectResponse> Update(long id, [FromBody] UpdateProjectRequest request, CancellationToken ct) =>
        await _projects.UpdateAsync(id, request, ct);

    [HttpPost("{id:long}/complete")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<ProjectResponse> Complete(long id, CancellationToken ct) =>
        await _projects.CompleteAsync(id, ct);

    /// <summary>
    /// Recompute project/feature progress for all tenant projects.
    /// Applies legacy effort policy: EstimatedHours ≤ 0 → weight 1.
    /// </summary>
    [HttpPost("recalculate-progress")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<ActionResult<object>> RecalculateProgress(CancellationToken ct)
    {
        var count = await _progress.RecalculateAllAsync(ct);
        return Ok(new { projectsRecalculated = count });
    }

    [HttpDelete("{id:long}")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _projects.DeleteAsync(id, ct);
        return NoContent();
    }
}
