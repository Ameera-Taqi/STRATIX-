using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stratix.Api.Auth;
using Stratix.Application.DTOs.Kpi;
using Stratix.Application.Services;

namespace Stratix.Api.Controllers;

[ApiController]
[Route("api/kpi")]
[Authorize(Policy = AuthPolicies.AllTenantUsers)]
public class KpiController : ControllerBase
{
    private readonly IKpiEvaluationService _kpi;

    public KpiController(IKpiEvaluationService kpi) => _kpi = kpi;

    [HttpGet("periods")]
    public async Task<IReadOnlyList<EvaluationPeriodResponse>> Periods(CancellationToken ct) =>
        await _kpi.GetPeriodsAsync(ct);

    [HttpPost("periods")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<ActionResult<EvaluationPeriodResponse>> CreatePeriod(
        [FromBody] CreateEvaluationPeriodRequest request, CancellationToken ct)
    {
        var created = await _kpi.CreatePeriodAsync(request, ct);
        return CreatedAtAction(nameof(Periods), created);
    }

    [HttpPost("periods/{id:long}/open")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<EvaluationPeriodResponse> OpenPeriod(long id, CancellationToken ct) =>
        await _kpi.OpenPeriodAsync(id, ct);

    [HttpPost("periods/{id:long}/close")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<EvaluationPeriodResponse> ClosePeriod(long id, CancellationToken ct) =>
        await _kpi.ClosePeriodAsync(id, ct);

    [HttpGet("definitions")]
    public async Task<IReadOnlyList<KpiDefinitionResponse>> Definitions(CancellationToken ct) =>
        await _kpi.GetDefinitionsAsync(ct);

    [HttpPut("definitions")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<KpiDefinitionResponse> UpsertDefinition(
        [FromBody] UpsertKpiDefinitionRequest request, CancellationToken ct) =>
        await _kpi.UpsertDefinitionAsync(request, ct);

    [HttpGet("evaluations")]
    public async Task<IReadOnlyList<EmployeeEvaluationResponse>> Evaluations(
        [FromQuery] long? periodId, CancellationToken ct) =>
        await _kpi.ListEvaluationsAsync(periodId, ct);

    [HttpPost("evaluations/ensure")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<EmployeeEvaluationResponse> Ensure(
        [FromQuery] long periodId, [FromQuery] long userId, CancellationToken ct) =>
        await _kpi.EnsureEvaluationAsync(periodId, userId, ct);

    [HttpPost("evaluations/{id:long}/calculate")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<EmployeeEvaluationResponse> Calculate(long id, CancellationToken ct) =>
        await _kpi.CalculateAsync(id, ct);

    [HttpPost("evaluations/{id:long}/submit")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<EmployeeEvaluationResponse> Submit(long id, CancellationToken ct) =>
        await _kpi.SubmitAsync(id, ct);

    [HttpPost("evaluations/{id:long}/review")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<EmployeeEvaluationResponse> Review(long id, CancellationToken ct) =>
        await _kpi.StartReviewAsync(id, ct);

    [HttpPost("evaluations/{id:long}/approve")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<EmployeeEvaluationResponse> Approve(long id, CancellationToken ct) =>
        await _kpi.ApproveAsync(id, ct);

    [HttpPost("evaluations/{id:long}/reject")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<EmployeeEvaluationResponse> Reject(
        long id, [FromBody] RejectEvaluationRequest request, CancellationToken ct) =>
        await _kpi.RejectAsync(id, request.Reason, ct);

    /// <summary>Formal unlock after APPROVED — reason required; writes audit log.</summary>
    [HttpPost("evaluations/{id:long}/reopen")]
    [Authorize(Policy = AuthPolicies.OrgAdmins)]
    public async Task<EmployeeEvaluationResponse> Reopen(
        long id, [FromBody] ReopenEvaluationRequest request, CancellationToken ct) =>
        await _kpi.ReopenAsync(id, request.Reason, ct);

    /// <summary>Adjust result actual/score while unlocked (blocked when APPROVED).</summary>
    [HttpPatch("results/{resultId:long}")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<EmployeeEvaluationResponse> AdjustResult(
        long resultId, [FromBody] AdjustKpiResultRequest request, CancellationToken ct) =>
        await _kpi.AdjustResultAsync(resultId, request, ct);

    /// <summary>Manager notes shown on the employee scorecard explanation.</summary>
    [HttpPatch("evaluations/{id:long}/notes")]
    [Authorize(Policy = AuthPolicies.ProjectManagers)]
    public async Task<EmployeeEvaluationResponse> UpdateNotes(
        long id, [FromBody] UpdateEvaluationNotesRequest request, CancellationToken ct) =>
        await _kpi.UpdateNotesAsync(id, request.Notes, ct);

    [HttpPost("task-quality")]
    [Authorize(Policy = AuthPolicies.TeamLeaders)]
    public async Task<TaskQualityEvaluationResponse> RateTask(
        [FromBody] CreateTaskQualityRequest request, CancellationToken ct) =>
        await _kpi.RateTaskQualityAsync(request, ct);

    [HttpGet("task-quality/{taskId:long}")]
    public async Task<IReadOnlyList<TaskQualityEvaluationResponse>> TaskQuality(long taskId, CancellationToken ct) =>
        await _kpi.GetTaskQualityAsync(taskId, ct);
}
