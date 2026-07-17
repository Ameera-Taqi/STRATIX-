using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.EmployeeKpis;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/employee-kpis")]
[Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class EmployeeKpisController : ControllerBase
{
    private readonly IEmployeeKpiService _service;

    public EmployeeKpisController(IEmployeeKpiService service) => _service = service;

    [HttpGet]
    public async Task<IReadOnlyList<EmployeeKpiResponse>> GetAll([FromQuery] long? userId, CancellationToken ct) =>
        await _service.GetAllAsync(userId, ct);

    [HttpGet("{id:long}")]
    public async Task<EmployeeKpiResponse> Get(long id, CancellationToken ct) => await _service.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,TEAM_LEADER")]
    public async Task<ActionResult<EmployeeKpiResponse>> Create([FromBody] CreateEmployeeKpiRequest request, CancellationToken ct)
    {
        var created = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,TEAM_LEADER")]
    public async Task<EmployeeKpiResponse> Update(long id, [FromBody] UpdateEmployeeKpiRequest request, CancellationToken ct) =>
        await _service.UpdateAsync(id, request, ct);

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
