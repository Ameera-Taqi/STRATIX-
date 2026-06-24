using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Risks;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/risks")]
[Authorize(Roles = "ADMIN,PROJECT_MANAGER,EMPLOYEE,TEAM_LEADER,EXECUTIVE_VIEWER")]
public class RisksController : ControllerBase
{
    private readonly IRiskService _risks;

    public RisksController(IRiskService risks) => _risks = risks;

    [HttpGet]
    public async Task<IReadOnlyList<RiskResponse>> GetAll(CancellationToken ct) => await _risks.GetAllAsync(ct);

    [HttpGet("dashboard-stats")]
    public async Task<RiskDashboardStatsResponse> DashboardStats(CancellationToken ct) =>
        await _risks.GetDashboardStatsAsync(ct);

    [HttpGet("heat-map")]
    public async Task<RiskHeatMapResponse> HeatMap(CancellationToken ct) => await _risks.GetHeatMapAsync(ct);

    [HttpGet("project/{projectId:long}")]
    public async Task<IReadOnlyList<RiskResponse>> ByProject(long projectId, CancellationToken ct) =>
        await _risks.GetByProjectAsync(projectId, ct);

    [HttpGet("owner/{ownerId:long}")]
    public async Task<IReadOnlyList<RiskResponse>> ByOwner(long ownerId, CancellationToken ct) =>
        await _risks.GetByOwnerAsync(ownerId, ct);

    [HttpGet("open")]
    public async Task<IReadOnlyList<RiskResponse>> Open(CancellationToken ct) => await _risks.GetOpenAsync(ct);

    [HttpGet("critical")]
    public async Task<IReadOnlyList<RiskResponse>> Critical(CancellationToken ct) => await _risks.GetCriticalAsync(ct);

    [HttpGet("{id:long}")]
    public async Task<RiskResponse> Get(long id, CancellationToken ct) => await _risks.GetByIdAsync(id, ct);

    [HttpPost]
    [Authorize(Roles = "ADMIN,PROJECT_MANAGER")]
    public async Task<ActionResult<RiskResponse>> Create([FromBody] CreateRiskRequest request, CancellationToken ct)
    {
        var risk = await _risks.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Get), new { id = risk.Id }, risk);
    }

    [HttpPut("{id:long}")]
    [Authorize(Roles = "ADMIN,PROJECT_MANAGER")]
    public async Task<RiskResponse> Update(long id, [FromBody] UpdateRiskRequest request, CancellationToken ct) =>
        await _risks.UpdateAsync(id, request, ct);

    [HttpDelete("{id:long}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(long id, CancellationToken ct)
    {
        await _risks.DeleteAsync(id, ct);
        return NoContent();
    }
}
