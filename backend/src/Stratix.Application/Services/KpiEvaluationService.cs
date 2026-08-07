using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Kpi;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Events;
using Stratix.Domain.Workflow;

namespace Stratix.Application.Services;

public interface IKpiEvaluationService
{
    Task<IReadOnlyList<EvaluationPeriodResponse>> GetPeriodsAsync(CancellationToken ct = default);
    Task<EvaluationPeriodResponse> CreatePeriodAsync(CreateEvaluationPeriodRequest request, CancellationToken ct = default);
    Task<EvaluationPeriodResponse> OpenPeriodAsync(long id, CancellationToken ct = default);
    Task<EvaluationPeriodResponse> ClosePeriodAsync(long id, CancellationToken ct = default);

    Task<IReadOnlyList<KpiDefinitionResponse>> GetDefinitionsAsync(CancellationToken ct = default);
    Task<KpiDefinitionResponse> UpsertDefinitionAsync(UpsertKpiDefinitionRequest request, CancellationToken ct = default);

    Task<EmployeeEvaluationResponse> EnsureEvaluationAsync(long periodId, long userId, CancellationToken ct = default);
    Task<EmployeeEvaluationResponse> CalculateAsync(long evaluationId, CancellationToken ct = default);
    Task<EmployeeEvaluationResponse> SubmitAsync(long evaluationId, CancellationToken ct = default);
    Task<EmployeeEvaluationResponse> StartReviewAsync(long evaluationId, CancellationToken ct = default);
    Task<EmployeeEvaluationResponse> ApproveAsync(long evaluationId, CancellationToken ct = default);
    Task<EmployeeEvaluationResponse> RejectAsync(long evaluationId, string reason, CancellationToken ct = default);
    /// <summary>Formal unlock of an APPROVED evaluation (reason + audit). Required before any further edits.</summary>
    Task<EmployeeEvaluationResponse> ReopenAsync(long evaluationId, string reason, CancellationToken ct = default);
    /// <summary>Adjust result values/score while evaluation is unlocked (not APPROVED).</summary>
    Task<EmployeeEvaluationResponse> AdjustResultAsync(long resultId, AdjustKpiResultRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<EmployeeEvaluationResponse>> ListEvaluationsAsync(long? periodId, CancellationToken ct = default);

    Task<TaskQualityEvaluationResponse> RateTaskQualityAsync(CreateTaskQualityRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TaskQualityEvaluationResponse>> GetTaskQualityAsync(long taskId, CancellationToken ct = default);
}

public class KpiEvaluationService : IKpiEvaluationService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDomainEventDispatcher _events;
    private readonly IAuditTrailService _audit;

    public KpiEvaluationService(
        IApplicationDbContext db,
        ICurrentUserService currentUser,
        IDomainEventDispatcher events,
        IAuditTrailService audit)
    {
        _db = db;
        _currentUser = currentUser;
        _events = events;
        _audit = audit;
    }

    public async Task<IReadOnlyList<EvaluationPeriodResponse>> GetPeriodsAsync(CancellationToken ct = default)
    {
        var periods = await _db.EvaluationPeriods.OrderByDescending(p => p.StartDate).ToListAsync(ct);
        var snaps = await _db.PeriodKpiSnapshots.ToListAsync(ct);
        return periods.Select(p => ToPeriod(p, snaps.Where(s => s.PeriodId == p.Id).ToList())).ToList();
    }

    public async Task<EvaluationPeriodResponse> CreatePeriodAsync(CreateEvaluationPeriodRequest request, CancellationToken ct = default)
    {
        if (request.EndDate < request.StartDate)
            throw new ArgumentException("End date must be on or after start date.");

        var period = new EvaluationPeriod
        {
            Name = request.Name.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = EvaluationPeriodStatus.DRAFT,
        };
        _db.Add(period);
        await _db.SaveChangesAsync(ct);
        return ToPeriod(period, []);
    }

    public async Task<EvaluationPeriodResponse> OpenPeriodAsync(long id, CancellationToken ct = default)
    {
        var period = await FindPeriodAsync(id, ct);
        await EnsureActiveWeightsValidAsync(ct);
        period.Status = EvaluationPeriodStatus.OPEN;
        // Freeze KPI defs once at period start — later definition edits must not rewrite this snapshot.
        if (!await _db.PeriodKpiSnapshots.AnyAsync(s => s.PeriodId == period.Id, ct))
            await CapturePeriodKpiSnapshotsAsync(period, ct);
        await _db.SaveChangesAsync(ct);
        var snaps = await _db.PeriodKpiSnapshots.Where(s => s.PeriodId == period.Id).ToListAsync(ct);
        return ToPeriod(period, snaps);
    }

    public async Task<EvaluationPeriodResponse> ClosePeriodAsync(long id, CancellationToken ct = default)
    {
        var period = await FindPeriodAsync(id, ct);
        period.Status = EvaluationPeriodStatus.CLOSED;
        await _db.SaveChangesAsync(ct);
        var snaps = await _db.PeriodKpiSnapshots.Where(s => s.PeriodId == period.Id).ToListAsync(ct);
        return ToPeriod(period, snaps);
    }

    public async Task<IReadOnlyList<KpiDefinitionResponse>> GetDefinitionsAsync(CancellationToken ct = default) =>
        await _db.KpiDefinitions.OrderBy(d => d.Code)
            .Select(d => ToDef(d)).ToListAsync(ct);

    public async Task<KpiDefinitionResponse> UpsertDefinitionAsync(UpsertKpiDefinitionRequest request, CancellationToken ct = default)
    {
        // Live definition edits never rewrite period snapshots or historical result snapshots.
        KpiWeightPolicy.EnsureValidWeight(request.Weight);

        var code = request.Code.Trim().ToUpperInvariant();
        UserRole? role = null;
        if (!string.IsNullOrWhiteSpace(request.AppliesToRole))
        {
            if (!Enum.TryParse<UserRole>(request.AppliesToRole.Trim(), true, out var parsed))
                throw new ArgumentException($"Unknown role '{request.AppliesToRole}'.");
            role = parsed;
        }

        var all = await _db.KpiDefinitions.Select(d => new { d.Id, d.Code, d.AppliesToRole, d.IsActive }).ToListAsync(ct);
        var existing = await _db.KpiDefinitions.FirstOrDefaultAsync(
            d => d.Code == code && d.AppliesToRole == role, ct);

        KpiWeightPolicy.EnsureNoDuplicateCodeForRole(
            all.Select(d => (d.Id, d.Code, d.AppliesToRole, d.IsActive)),
            existing?.Id,
            code,
            role);

        if (existing is null)
        {
            existing = new KpiDefinition { Code = code, AppliesToRole = role };
            _db.Add(existing);
        }

        existing.Name = request.Name.Trim();
        existing.Description = request.Description;
        existing.Weight = request.Weight;
        existing.TargetValue = request.TargetValue;
        existing.Formula = string.IsNullOrWhiteSpace(request.Formula) ? null : request.Formula.Trim();
        existing.HigherIsBetter = request.HigherIsBetter;
        existing.IsActive = request.IsActive;
        existing.AppliesToRole = role;
        await _db.SaveChangesAsync(ct);
        return ToDef(existing);
    }

    public async Task<EmployeeEvaluationResponse> EnsureEvaluationAsync(long periodId, long userId, CancellationToken ct = default)
    {
        var period = await FindPeriodAsync(periodId, ct);
        if (!await _db.Users.AnyAsync(u => u.Id == userId, ct))
            throw new KeyNotFoundException("User not found");

        var eval = await _db.EmployeeEvaluations
            .Include(e => e.Results).ThenInclude(r => r.KpiDefinition)
            .Include(e => e.User)
            .Include(e => e.Period)
            .FirstOrDefaultAsync(e => e.PeriodId == periodId && e.UserId == userId, ct);

        if (eval is null)
        {
            eval = new EmployeeEvaluation
            {
                PeriodId = periodId,
                UserId = userId,
                Status = EmployeeEvaluationStatus.DRAFT,
                OrganizationId = period.OrganizationId,
            };
            _db.Add(eval);
            await _db.SaveChangesAsync(ct);
            eval = await LoadEvaluationAsync(eval.Id, ct);
        }

        return ToEval(eval!);
    }

    public async Task<EmployeeEvaluationResponse> CalculateAsync(long evaluationId, CancellationToken ct = default)
    {
        var eval = await LoadEvaluationAsync(evaluationId, ct)
            ?? throw new KeyNotFoundException("Evaluation not found");
        EvaluationLockPolicy.EnsureCanRecalculate(eval);

        var period = eval.Period ?? await FindPeriodAsync(eval.PeriodId, ct);
        await EnsureActiveWeightsValidAsync(ct);
        // Prefer period-frozen KPI defs; capture once if the period was opened before snapshots existed.
        var frozenAll = await EnsurePeriodSnapshotsAsync(period, ct);
        var userRole = eval.User?.Role
            ?? (await _db.Users.Where(u => u.Id == eval.UserId).Select(u => (UserRole?)u.Role).FirstOrDefaultAsync(ct));
        var roleSpecific = frozenAll.Where(s => s.AppliesToRole == userRole).ToList();
        var frozen = roleSpecific.Count > 0
            ? roleSpecific
            : frozenAll.Where(s => s.AppliesToRole is null).ToList();
        KpiWeightPolicy.EnsureActiveWeightsTotalOneHundred(
            frozen.Select(s => (s.Code, s.Weight, s.AppliesToRole, true)));
        if (frozen.Count == 0)
            throw new InvalidOperationException("No active KPI definitions. Create definitions before calculating.");

        var tasks = await _db.Tasks.ActiveOnly()
            .Where(t => t.AssigneeId == eval.UserId &&
                        t.CreatedAt >= period.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) &&
                        t.CreatedAt <= period.EndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc))
            .ToListAsync(ct);

        var qualityScores = await _db.TaskQualityEvaluations
            .Where(q => q.Task != null && !q.Task.IsDeleted && q.Task.AssigneeId == eval.UserId)
            .Select(q => q.QualityScore)
            .ToListAsync(ct);
        var qualityAvg = qualityScores.Count == 0 ? 0m : (decimal)qualityScores.Average();

        foreach (var old in eval.Results.ToList())
            _db.Remove(old);

        decimal weighted = 0, weightSum = 0;
        foreach (var snap in frozen)
        {
            var (value, score) = CalculateKpi(snap.Formula, snap.HigherIsBetter, tasks, qualityAvg);
            var weight = snap.Weight > 0 ? snap.Weight : 1m;
            var result = new EmployeeKpiResult
            {
                OrganizationId = eval.OrganizationId,
                EvaluationId = eval.Id,
                KpiDefinitionId = snap.KpiDefinitionId,
                SnapshotName = snap.Name,
                SnapshotCode = snap.Code,
                SnapshotWeight = weight,
                SnapshotTarget = snap.TargetValue,
                SnapshotFormula = snap.Formula,
                SnapshotHigherIsBetter = snap.HigherIsBetter,
                CalculatedValue = value,
                Score = score,
            };
            _db.Add(result);
            weighted += score * weight;
            weightSum += weight;
        }

        eval.OverallScore = weightSum <= 0 ? 0 : Math.Round(weighted / weightSum, 2);
        eval.Status = EmployeeEvaluationStatus.DRAFT;
        await _db.SaveChangesAsync(ct);
        return ToEval(await LoadEvaluationAsync(eval.Id, ct)!);
    }

    public async Task<EmployeeEvaluationResponse> SubmitAsync(long evaluationId, CancellationToken ct = default)
    {
        var eval = await LoadEvaluationAsync(evaluationId, ct)
            ?? throw new KeyNotFoundException("Evaluation not found");
        EvaluationLockPolicy.EnsureNotLocked(eval);
        if (eval.Results.Count == 0)
            throw new InvalidOperationException("Calculate KPI results before submitting.");

        eval.Status = EmployeeEvaluationStatus.SUBMITTED;
        eval.SubmittedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _events.DispatchAsync(new EvaluationSubmittedEvent(
            eval.OrganizationId, eval.Id, eval.UserId, eval.PeriodId,
            _currentUser.UserId ?? 0, DateTimeOffset.UtcNow), ct);

        return ToEval(await LoadEvaluationAsync(eval.Id, ct)!);
    }

    public async Task<EmployeeEvaluationResponse> StartReviewAsync(long evaluationId, CancellationToken ct = default)
    {
        var eval = await LoadEvaluationAsync(evaluationId, ct)
            ?? throw new KeyNotFoundException("Evaluation not found");
        EvaluationLockPolicy.EnsureNotLocked(eval);
        if (eval.Status is not (EmployeeEvaluationStatus.SUBMITTED or EmployeeEvaluationStatus.IN_REVIEW))
            throw new InvalidOperationException("Only submitted evaluations can enter review.");

        eval.Status = EmployeeEvaluationStatus.IN_REVIEW;
        eval.ReviewedById = _currentUser.UserId;
        eval.ReviewedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToEval(await LoadEvaluationAsync(eval.Id, ct)!);
    }

    public async Task<EmployeeEvaluationResponse> ApproveAsync(long evaluationId, CancellationToken ct = default)
    {
        var eval = await LoadEvaluationAsync(evaluationId, ct)
            ?? throw new KeyNotFoundException("Evaluation not found");
        if (eval.Status is not (EmployeeEvaluationStatus.SUBMITTED or EmployeeEvaluationStatus.IN_REVIEW))
            throw new InvalidOperationException("Evaluation is not ready for approval.");

        var oldValues = Snapshot(eval);
        eval.Status = EmployeeEvaluationStatus.APPROVED;
        eval.ApprovedById = _currentUser.UserId;
        eval.ApprovedAt = DateTimeOffset.UtcNow;
        eval.RejectionReason = null;
        eval.ReopenReason = null;
        await _db.SaveChangesAsync(ct);

        await _audit.RecordUpdateAsync(
            AuditEntityType.EVALUATION,
            eval.Id,
            EvalName(eval),
            oldValues,
            Snapshot(eval),
            $"Evaluation approved (final score {eval.OverallScore})",
            null,
            null,
            ct);

        return ToEval(await LoadEvaluationAsync(eval.Id, ct)!);
    }

    public async Task<EmployeeEvaluationResponse> RejectAsync(long evaluationId, string reason, CancellationToken ct = default)
    {
        var eval = await LoadEvaluationAsync(evaluationId, ct)
            ?? throw new KeyNotFoundException("Evaluation not found");
        EvaluationLockPolicy.EnsureNotLocked(eval);
        if (eval.Status is not (EmployeeEvaluationStatus.SUBMITTED or EmployeeEvaluationStatus.IN_REVIEW or EmployeeEvaluationStatus.DRAFT))
            throw new InvalidOperationException("Only draft/submitted/in-review evaluations can be rejected.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.");

        eval.Status = EmployeeEvaluationStatus.REJECTED;
        eval.RejectionReason = reason.Trim();
        eval.ReviewedById = _currentUser.UserId;
        eval.ReviewedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToEval(await LoadEvaluationAsync(eval.Id, ct)!);
    }

    public async Task<EmployeeEvaluationResponse> ReopenAsync(long evaluationId, string reason, CancellationToken ct = default)
    {
        var eval = await LoadEvaluationAsync(evaluationId, ct)
            ?? throw new KeyNotFoundException("Evaluation not found");
        EvaluationLockPolicy.EnsureCanReopen(eval, reason);

        var oldValues = Snapshot(eval);
        var trimmed = reason.Trim();
        eval.Status = EmployeeEvaluationStatus.DRAFT;
        eval.ReopenReason = trimmed;
        eval.ApprovedById = null;
        eval.ApprovedAt = null;
        // Keep OverallScore + Results intact until Calculate/Adjust; they remain editable after unlock.
        await _db.SaveChangesAsync(ct);

        await _audit.RecordUpdateAsync(
            AuditEntityType.EVALUATION,
            eval.Id,
            EvalName(eval),
            oldValues,
            Snapshot(eval),
            $"Evaluation reopened: {trimmed}",
            null,
            null,
            ct);

        return ToEval(await LoadEvaluationAsync(eval.Id, ct)!);
    }

    public async Task<EmployeeEvaluationResponse> AdjustResultAsync(long resultId, AdjustKpiResultRequest request, CancellationToken ct = default)
    {
        var result = await _db.EmployeeKpiResults.FirstOrDefaultAsync(r => r.Id == resultId, ct)
            ?? throw new KeyNotFoundException("KPI result not found");

        var eval = await LoadEvaluationAsync(result.EvaluationId, ct)
            ?? throw new KeyNotFoundException("Evaluation not found");
        EvaluationLockPolicy.EnsureCanAdjust(eval);

        var oldValues = Snapshot(eval);
        var tracked = eval.Results.First(r => r.Id == resultId);

        if (request.AdjustedValue.HasValue)
            tracked.AdjustedValue = request.AdjustedValue;
        if (request.Score.HasValue)
            tracked.Score = request.Score.Value;
        if (request.Comment is not null)
            tracked.Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim();

        decimal weighted = 0, weightSum = 0;
        foreach (var r in eval.Results)
        {
            var w = r.SnapshotWeight > 0 ? r.SnapshotWeight : 1m;
            weighted += r.Score * w;
            weightSum += w;
        }
        eval.OverallScore = weightSum <= 0 ? 0 : Math.Round(weighted / weightSum, 2);
        await _db.SaveChangesAsync(ct);

        var reloaded = (await LoadEvaluationAsync(eval.Id, ct))!;
        await _audit.RecordUpdateAsync(
            AuditEntityType.EVALUATION,
            eval.Id,
            EvalName(eval),
            oldValues,
            Snapshot(reloaded),
            $"KPI result {resultId} adjusted",
            null,
            null,
            ct);

        return ToEval(reloaded);
    }

    public async Task<IReadOnlyList<EmployeeEvaluationResponse>> ListEvaluationsAsync(long? periodId, CancellationToken ct = default)
    {
        var q = _db.EmployeeEvaluations
            .Include(e => e.Results).ThenInclude(r => r.KpiDefinition)
            .Include(e => e.User)
            .Include(e => e.Period)
            .AsQueryable();
        if (periodId.HasValue) q = q.Where(e => e.PeriodId == periodId);
        var rows = await q.OrderByDescending(e => e.UpdatedAt).ToListAsync(ct);
        return rows.Select(ToEval).ToList();
    }

    public async Task<TaskQualityEvaluationResponse> RateTaskQualityAsync(CreateTaskQualityRequest request, CancellationToken ct = default)
    {
        if (request.QualityScore is < 1 or > 5)
            throw new ArgumentException("Quality score must be between 1 and 5.");

        var task = await _db.Tasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, ct)
            ?? throw new KeyNotFoundException("Task not found");

        var row = new TaskQualityEvaluation
        {
            OrganizationId = task.OrganizationId,
            TaskId = task.Id,
            EvaluatorId = _currentUser.UserId ?? throw new UnauthorizedAccessException(),
            QualityScore = request.QualityScore,
            Notes = request.Notes,
        };
        _db.Add(row);
        await _db.SaveChangesAsync(ct);
        return new TaskQualityEvaluationResponse(row.Id, row.TaskId, row.EvaluatorId, row.QualityScore, row.Notes, row.CreatedAt);
    }

    public async Task<IReadOnlyList<TaskQualityEvaluationResponse>> GetTaskQualityAsync(long taskId, CancellationToken ct = default) =>
        await _db.TaskQualityEvaluations.Where(q => q.TaskId == taskId)
            .OrderByDescending(q => q.CreatedAt)
            .Select(q => new TaskQualityEvaluationResponse(q.Id, q.TaskId, q.EvaluatorId, q.QualityScore, q.Notes, q.CreatedAt))
            .ToListAsync(ct);

    private async Task CapturePeriodKpiSnapshotsAsync(EvaluationPeriod period, CancellationToken ct)
    {
        var existing = await _db.PeriodKpiSnapshots.Where(s => s.PeriodId == period.Id).ToListAsync(ct);
        foreach (var row in existing)
            _db.Remove(row);

        var defs = await _db.KpiDefinitions.Where(d => d.IsActive).ToListAsync(ct);
        foreach (var def in defs)
        {
            _db.Add(new PeriodKpiSnapshot
            {
                OrganizationId = period.OrganizationId,
                PeriodId = period.Id,
                KpiDefinitionId = def.Id,
                Code = def.Code,
                Name = def.Name,
                Weight = def.Weight > 0 ? def.Weight : 1m,
                TargetValue = def.TargetValue,
                Formula = string.IsNullOrWhiteSpace(def.Formula) ? def.Code : def.Formula.Trim(),
                HigherIsBetter = def.HigherIsBetter,
                AppliesToRole = def.AppliesToRole,
            });
        }
    }

    private async Task EnsureActiveWeightsValidAsync(CancellationToken ct)
    {
        var defs = await _db.KpiDefinitions
            .Select(d => new { d.Code, d.Weight, d.AppliesToRole, d.IsActive })
            .ToListAsync(ct);
        KpiWeightPolicy.EnsureActiveWeightsTotalOneHundred(
            defs.Select(d => (d.Code, d.Weight, d.AppliesToRole, d.IsActive)));
    }

    /// <summary>Returns frozen KPIs for the period, capturing from live defs if none exist yet.</summary>
    private async Task<List<PeriodKpiSnapshot>> EnsurePeriodSnapshotsAsync(EvaluationPeriod period, CancellationToken ct)
    {
        var snaps = await _db.PeriodKpiSnapshots.Where(s => s.PeriodId == period.Id).ToListAsync(ct);
        if (snaps.Count > 0) return snaps;

        await CapturePeriodKpiSnapshotsAsync(period, ct);
        await _db.SaveChangesAsync(ct);
        return await _db.PeriodKpiSnapshots.Where(s => s.PeriodId == period.Id).ToListAsync(ct);
    }

    private static (decimal value, decimal score) CalculateKpi(
        string formula,
        bool higherIsBetter,
        List<TaskItem> tasks,
        decimal qualityAvg)
    {
        var code = formula.Trim().ToUpperInvariant();
        var completed = tasks.Where(TaskCompletionPolicy.CountsAsCompleted).ToList();
        decimal value = code switch
        {
            "TASKS_COMPLETED" => completed.Count,
            "TASKS_ON_TIME" => completed.Count(t =>
                t.DueDate is null ||
                (t.CompletedAt is { } c && DateOnly.FromDateTime(c.UtcDateTime) <= t.DueDate)),
            "TASK_QUALITY" => qualityAvg,
            "EFFORT_HOURS" => completed.Sum(t => t.EstimatedHours),
            _ => completed.Count,
        };

        decimal score = code switch
        {
            "TASK_QUALITY" => qualityAvg <= 0 ? 0 : Math.Round(qualityAvg / 5m * 100m, 2),
            "TASKS_COMPLETED" or "TASKS_ON_TIME" => Math.Min(100m, value * 10m),
            "EFFORT_HOURS" => Math.Min(100m, value * 5m),
            _ => Math.Min(100m, value * 10m),
        };

        if (!higherIsBetter)
            score = Math.Max(0, 100 - score);

        return (value, score);
    }

    private async Task<EvaluationPeriod> FindPeriodAsync(long id, CancellationToken ct) =>
        await _db.EvaluationPeriods.FirstOrDefaultAsync(p => p.Id == id, ct)
        ?? throw new KeyNotFoundException("Evaluation period not found");

    private async Task<EmployeeEvaluation?> LoadEvaluationAsync(long id, CancellationToken ct) =>
        await _db.EmployeeEvaluations
            .Include(e => e.Results).ThenInclude(r => r.KpiDefinition)
            .Include(e => e.User)
            .Include(e => e.Period)
            .FirstOrDefaultAsync(e => e.Id == id, ct);

    private static string EvalName(EmployeeEvaluation e) =>
        $"{e.Period?.Name ?? $"Period {e.PeriodId}"} / {e.User?.Name ?? $"User {e.UserId}"}";

    private static string Snapshot(EmployeeEvaluation e) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            Status = e.Status.ToString(),
            e.OverallScore,
            e.ApprovedAt,
            e.RejectionReason,
            e.ReopenReason,
            Results = e.Results.Select(r => new
            {
                r.Id,
                r.KpiDefinitionId,
                r.SnapshotName,
                r.SnapshotCode,
                r.SnapshotWeight,
                r.SnapshotTarget,
                r.SnapshotFormula,
                r.CalculatedValue,
                r.AdjustedValue,
                r.Score,
            }),
        });

    private static EvaluationPeriodResponse ToPeriod(EvaluationPeriod p, IReadOnlyList<PeriodKpiSnapshot> snaps) =>
        new(
            p.Id, p.Name, p.StartDate, p.EndDate, p.Status.ToString(), p.CreatedAt, p.UpdatedAt,
            snaps.Select(s => new PeriodKpiSnapshotResponse(
                s.Id, s.KpiDefinitionId, s.Code, s.Name, s.Weight, s.TargetValue, s.Formula, s.HigherIsBetter,
                s.AppliesToRole?.ToString())).ToList());

    private static KpiDefinitionResponse ToDef(KpiDefinition d) =>
        new(d.Id, d.Code, d.Name, d.Description, d.Weight, d.TargetValue, d.Formula, d.HigherIsBetter, d.IsActive,
            d.AppliesToRole?.ToString());

    private static EmployeeEvaluationResponse ToEval(EmployeeEvaluation e) =>
        new(
            e.Id,
            e.PeriodId,
            e.Period?.Name ?? "",
            e.UserId,
            e.User?.Name ?? "",
            e.Status.ToString(),
            e.OverallScore,
            e.Notes,
            e.SubmittedAt,
            e.ReviewedAt,
            e.ApprovedAt,
            e.RejectionReason,
            e.ReopenReason,
            e.Results.Select(r => new EmployeeKpiResultResponse(
                r.Id,
                r.KpiDefinitionId,
                string.IsNullOrEmpty(r.SnapshotCode) ? (r.KpiDefinition?.Code ?? "") : r.SnapshotCode,
                string.IsNullOrEmpty(r.SnapshotName) ? (r.KpiDefinition?.Name ?? "") : r.SnapshotName,
                r.SnapshotWeight > 0 ? r.SnapshotWeight : (r.KpiDefinition?.Weight ?? 1m),
                r.SnapshotTarget,
                string.IsNullOrEmpty(r.SnapshotFormula) ? (r.KpiDefinition?.Formula ?? r.KpiDefinition?.Code ?? "") : r.SnapshotFormula,
                r.SnapshotHigherIsBetter,
                r.CalculatedValue, r.AdjustedValue, r.Score, r.Comment)).ToList());
}
