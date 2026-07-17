using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Departments;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize]
public class DepartmentsController : ControllerBase
{
    private readonly IDepartmentService _departments;

    public DepartmentsController(IDepartmentService departments) => _departments = departments;

    [HttpGet]
    public async Task<IReadOnlyList<DepartmentResponse>> GetAll(CancellationToken ct) =>
        await _departments.GetAllAsync(ct);

    [HttpPost]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<ActionResult<DepartmentResponse>> Create([FromBody] CreateDepartmentRequest request, CancellationToken ct)
    {
        var department = await _departments.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetAll), department);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<DepartmentResponse> Update(long id, [FromBody] UpdateDepartmentRequest request, CancellationToken ct) =>
        await _departments.UpdateAsync(id, request, ct);

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _departments.DeleteAsync(id, ct);
        return NoContent();
    }
}
