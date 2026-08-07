using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Stratix.Application.DTOs.Ai;
using Stratix.Application.Interfaces;
using Stratix.Application.Observability;
using Stratix.Application.Services;
using Stratix.Api.Auth;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IProjectHealthAnalysisService _analysis;
    private readonly IPlanLimitService _planLimits;
    private readonly IStratixMetrics _metrics;
    private readonly ILogger<AiController> _logger;

    public AiController(
        IProjectHealthAnalysisService analysis,
        IPlanLimitService planLimits,
        IStratixMetrics metrics,
        ILogger<AiController> logger)
    {
        _analysis = analysis;
        _planLimits = planLimits;
        _metrics = metrics;
        _logger = logger;
    }

    [HttpPost("project-health-analysis")]
    [Authorize(Policy = AuthPolicies.AiAnalysts)]
    public async Task<ProjectHealthAnalysisResponse> Analyze([FromBody] ProjectHealthAnalysisRequest request, CancellationToken ct)
    {
        try
        {
            await _planLimits.EnsureAiEnabledAsync(ct);
            return await _analysis.AnalyzeAsync(request, ct);
        }
        catch (PlanLimitExceededException)
        {
            _metrics.RecordAiAnalysisFailure("plan_disabled");
            _logger.LogWarning("AI analysis rejected — plan does not include AI");
            throw;
        }
        catch (KeyNotFoundException)
        {
            _metrics.RecordAiAnalysisFailure("project_not_found");
            _logger.LogWarning("AI analysis failed — project not found");
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _metrics.RecordAiAnalysisFailure("unexpected");
            _logger.LogError(ex, "AI analysis failed unexpectedly");
            throw;
        }
    }
}
