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

    [HttpGet("health")]
    [AllowAnonymous]
    public async Task<HealthResponse> Get(CancellationToken ct) => await _health.GetHealthAsync(ct);
}
