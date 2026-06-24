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
}
