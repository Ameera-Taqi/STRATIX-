using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api")]
public class HealthController : ControllerBase
{
    private readonly IHealthService _health;

    public HealthController(IHealthService health) => _health = health;

    /// <summary>Legacy combined check (maps to readiness). Prefer /health/live and /health/ready.</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<ActionResult<HealthResponse>> Get(CancellationToken ct)
    {
        var result = await _health.GetHealthAsync(ct);
        return result.Status == "UP" ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }

    /// <summary>Process is up — no dependency checks.</summary>
    [HttpGet("health/live")]
    [AllowAnonymous]
    public async Task<ActionResult<LivenessResponse>> Live(CancellationToken ct)
    {
        var result = await _health.GetLivenessAsync(ct);
        return Ok(result);
    }

    /// <summary>Ready to serve traffic: SQL Server, storage, required migrations.</summary>
    [HttpGet("health/ready")]
    [AllowAnonymous]
    public async Task<ActionResult<ReadinessResponse>> Ready(CancellationToken ct)
    {
        var result = await _health.GetReadinessAsync(ct);
        return result.Status == "UP" ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }

    /// <summary>In-process counter snapshot (no secrets). Prefer scraping System.Diagnostics.Metrics in production.</summary>
    [HttpGet("metrics")]
    [AllowAnonymous]
    public ActionResult<object> Metrics([FromServices] Stratix.Application.Observability.IStratixMetrics metrics) =>
        Ok(new
        {
            meter = Stratix.Application.Observability.StratixMetrics.MeterName,
            counters = metrics.Snapshot()
        });
}
