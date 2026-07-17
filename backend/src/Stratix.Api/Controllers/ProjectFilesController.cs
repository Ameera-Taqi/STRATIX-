using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.ProjectFiles;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,EMPLOYEE,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class ProjectFilesController : ControllerBase
{
    private readonly IProjectFileService _service;

    public ProjectFilesController(IProjectFileService service) => _service = service;

    [HttpGet("projects/{projectId:long}/files")]
    public async Task<IReadOnlyList<ProjectFileResponse>> ByProject(long projectId, CancellationToken ct) =>
        await _service.GetByProjectAsync(projectId, ct);

    [HttpPost("project-files")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,TEAM_LEADER,EMPLOYEE")]
    public async Task<ActionResult<ProjectFileResponse>> Create([FromBody] CreateProjectFileRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, created);
    }

    [HttpDelete("project-files/{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
