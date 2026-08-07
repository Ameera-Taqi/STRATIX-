using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Ai;
using Stratix.Application.Interfaces;
using Stratix.Application.Services;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IProjectHealthAnalysisService _analysis;
    private readonly IPlanLimitService _planLimits;

    public AiController(IProjectHealthAnalysisService analysis, IPlanLimitService planLimits)
    {
        _analysis = analysis;
        _planLimits = planLimits;
    }

    [HttpPost("project-health-analysis")]
    [Authorize(Policy = AuthPolicies.AiAnalysts)]
    public async Task<ProjectHealthAnalysisResponse> Analyze([FromBody] ProjectHealthAnalysisRequest request, CancellationToken ct)
    {
        await _planLimits.EnsureAiEnabledAsync(ct);
        return await _analysis.AnalyzeAsync(request, ct);
    }
}
