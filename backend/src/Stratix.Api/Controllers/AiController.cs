using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Application.DTOs.Ai;
using Stratix.Application.Interfaces;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/ai")]
public class AiController : ControllerBase
{
    private readonly IProjectHealthAnalysisService _analysis;

    public AiController(IProjectHealthAnalysisService analysis) => _analysis = analysis;

    [HttpPost("project-health-analysis")]
    [Authorize(Roles = "SUPER_ADMIN,ORG_ADMIN,ADMIN,PROJECT_MANAGER,EXECUTIVE_VIEWER,TEAM_LEADER")]
    public async Task<ProjectHealthAnalysisResponse> Analyze([FromBody] ProjectHealthAnalysisRequest request, CancellationToken ct) =>
        await _analysis.AnalyzeAsync(request, ct);
}
