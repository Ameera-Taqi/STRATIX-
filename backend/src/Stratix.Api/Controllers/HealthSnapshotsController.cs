using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Api.Auth;
using Stratix.Application.DTOs.Health;
using Stratix.Application.Services;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/health-snapshots")]
[Authorize(Policy = AuthPolicies.AllTenantUsers)]
public class HealthSnapshotsController : ControllerBase
{
    private readonly IProjectHealthSnapshotService _snapshots;

    public HealthSnapshotsController(IProjectHealthSnapshotService snapshots) => _snapshots = snapshots;

    [HttpGet("dashboard")]
    public async Task<IReadOnlyList<ProjectHealthSnapshotResponse>> Dashboard(CancellationToken ct) =>
        await _snapshots.GetDashboardAsync(ct);

    [HttpGet("projects/{projectId:long}/latest")]
    public async Task<ActionResult<ProjectHealthSnapshotResponse>> Latest(long projectId, CancellationToken ct)
    {
        var snap = await _snapshots.GetLatestAsync(projectId, ct);
        return snap is null ? NotFound() : Ok(snap);
    }

    [HttpGet("projects/{projectId:long}")]
    public async Task<IReadOnlyList<ProjectHealthSnapshotResponse>> History(
        long projectId, [FromQuery] int take = 30, CancellationToken ct = default) =>
        await _snapshots.GetHistoryAsync(projectId, take, ct);

    [HttpPost("projects/{projectId:long}/capture")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<ProjectHealthSnapshotResponse> Capture(long projectId, CancellationToken ct) =>
        await _snapshots.CaptureAsync(projectId, ct);
}
