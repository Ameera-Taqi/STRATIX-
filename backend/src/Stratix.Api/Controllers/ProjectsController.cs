using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/projects")]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,EMPLOYEE,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projects;

    public ProjectsController(IProjectService projects) => _projects = projects;

    [HttpGet]
    public async Task<IReadOnlyList<ProjectResponse>> GetAll([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        if (pageSize is > 0)
        {
            var paged = await _projects.GetPagedAsync(page ?? 1, pageSize.Value, ct);
            PagingHeaders.Apply(Response, paged.Total, paged.Page, paged.PageSize, paged.TotalPages);
            return paged.Items;
        }
        return await _projects.GetAllAsync(ct);
    }

    [HttpGet("{id:long}")]
    public async Task<ProjectResponse> Get(long id, CancellationToken ct) => await _projects.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER")]
    public async Task<ActionResult<ProjectResponse>> Create([FromBody] CreateProjectRequest request, CancellationToken ct)
    {
        var project = await _projects.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = project.Id }, project);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER")]
    public async Task<ProjectResponse> Update(long id, [FromBody] UpdateProjectRequest request, CancellationToken ct) =>
        await _projects.UpdateAsync(id, request, ct);

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _projects.DeleteAsync(id, ct);
        return NoContent();
    }
}
