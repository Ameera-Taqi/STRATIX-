using Microsoft.EntityFrameworkCore;
using Stratix.Application.DTOs.Kpi;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Events;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

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
    Task<IReadOnlyList<EmployeeEvaluationResponse>> ListEvaluationsAsync(long? periodId, CancellationToken ct = default);

    Task<TaskQualityEvaluationResponse> RateTaskQualityAsync(CreateTaskQualityRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<TaskQualityEvaluationResponse>> GetTaskQualityAsync(long taskId, CancellationToken ct = default);
}

public class KpiEvaluationService : IKpiEvaluationService
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IDomainEventDispatcher _events;

    public KpiEvaluationService(IApplicationDbContext db, ICurrentUserService currentUser, IDomainEventDispatcher events)
    {
        _db = db;
        _currentUser = currentUser;
        _events = events;
    }

    public async Task<IReadOnlyList<EvaluationPeriodResponse>> GetPeriodsAsync(CancellationToken ct = default) =>
        await _db.EvaluationPeriods.OrderByDescending(p => p.StartDate)
            .Select(p => ToPeriod(p)).ToListAsync(ct);

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
        return ToPeriod(period);
    }

    public async Task<EvaluationPeriodResponse> OpenPeriodAsync(long id, CancellationToken ct = default)
    {
        var period = await FindPeriodAsync(id, ct);
        period.Status = EvaluationPeriodStatus.OPEN;
        await _db.SaveChangesAsync(ct);
        return ToPeriod(period);
    }

    public async Task<EvaluationPeriodResponse> ClosePeriodAsync(long id, CancellationToken ct = default)
    {
        var period = await FindPeriodAsync(id, ct);
        period.Status = EvaluationPeriodStatus.CLOSED;
        await _db.SaveChangesAsync(ct);
        return ToPeriod(period);
    }

    public async Task<IReadOnlyList<KpiDefinitionResponse>> GetDefinitionsAsync(CancellationToken ct = default) =>
        await _db.KpiDefinitions.OrderBy(d => d.Code)
            .Select(d => ToDef(d)).ToListAsync(ct);

    public async Task<KpiDefinitionResponse> UpsertDefinitionAsync(UpsertKpiDefinitionRequest request, CancellationToken ct = default)
    {
        var code = request.Code.Trim().ToUpperInvariant();
        var existing = await _db.KpiDefinitions.FirstOrDefaultAsync(d => d.Code == code, ct);
        if (existing is null)
        {
            existing = new KpiDefinition { Code = code };
            _db.Add(existing);
        }
        existing.Name = request.Name.Trim();
        existing.Description = request.Description;
        existing.Weight = request.Weight <= 0 ? 1 : request.Weight;
        existing.HigherIsBetter = request.HigherIsBetter;
        existing.IsActive = request.IsActive;
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
        EnsureEditable(eval);

        var period = eval.Period ?? await FindPeriodAsync(eval.PeriodId, ct);
        var defs = await _db.KpiDefinitions.Where(d => d.IsActive).ToListAsync(ct);
        if (defs.Count == 0)
            throw new InvalidOperationException("No active KPI definitions. Create definitions before calculating.");

        var tasks = await _db.Tasks
            .Where(t => t.AssigneeId == eval.UserId &&
                        t.CreatedAt >= period.StartDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) &&
                        t.CreatedAt <= period.EndDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc))
            .ToListAsync(ct);

        var qualityScores = await _db.TaskQualityEvaluations
            .Where(q => q.Task!.AssigneeId == eval.UserId)
            .Select(q => q.QualityScore)
            .ToListAsync(ct);
        var qualityAvg = qualityScores.Count == 0 ? 0m : (decimal)qualityScores.Average();

        // Clear previous results
        foreach (var old in eval.Results.ToList())
            _db.Remove(old);

        decimal weighted = 0, weightSum = 0;
        foreach (var def in defs)
        {
            var (value, score) = CalculateKpi(def, tasks, qualityAvg);
            var result = new EmployeeKpiResult
            {
                OrganizationId = eval.OrganizationId,
                EvaluationId = eval.Id,
                KpiDefinitionId = def.Id,
                CalculatedValue = value,
                Score = score,
            };
            _db.Add(result);
            weighted += score * def.Weight;
            weightSum += def.Weight;
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

        eval.Status = EmployeeEvaluationStatus.APPROVED;
        eval.ApprovedById = _currentUser.UserId;
        eval.ApprovedAt = DateTimeOffset.UtcNow;
        eval.RejectionReason = null;
        await _db.SaveChangesAsync(ct);
        return ToEval(await LoadEvaluationAsync(eval.Id, ct)!);
    }

    public async Task<EmployeeEvaluationResponse> RejectAsync(long evaluationId, string reason, CancellationToken ct = default)
    {
        var eval = await LoadEvaluationAsync(evaluationId, ct)
            ?? throw new KeyNotFoundException("Evaluation not found");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.");

        eval.Status = EmployeeEvaluationStatus.REJECTED;
        eval.RejectionReason = reason.Trim();
        eval.ReviewedById = _currentUser.UserId;
        eval.ReviewedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return ToEval(await LoadEvaluationAsync(eval.Id, ct)!);
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

    private static (decimal value, decimal score) CalculateKpi(KpiDefinition def, List<TaskItem> tasks, decimal qualityAvg)
    {
        var code = def.Code.ToUpperInvariant();
        decimal value = code switch
        {
            "TASKS_COMPLETED" => tasks.Count(t => t.Status == DomainTaskStatus.DONE),
            "TASKS_ON_TIME" => tasks.Count(t =>
                t.Status == DomainTaskStatus.DONE &&
                (t.DueDate is null || (t.CompletedAt is { } c && DateOnly.FromDateTime(c.UtcDateTime) <= t.DueDate))),
            "TASK_QUALITY" => qualityAvg,
            "EFFORT_HOURS" => tasks.Where(t => t.Status == DomainTaskStatus.DONE).Sum(t => t.EstimatedHours),
            _ => tasks.Count(t => t.Status == DomainTaskStatus.DONE),
        };

        // Normalize into 0..100 score heuristically.
        decimal score = code switch
        {
            "TASK_QUALITY" => qualityAvg <= 0 ? 0 : Math.Round(qualityAvg / 5m * 100m, 2),
            "TASKS_COMPLETED" or "TASKS_ON_TIME" => Math.Min(100m, value * 10m),
            "EFFORT_HOURS" => Math.Min(100m, value * 5m),
            _ => Math.Min(100m, value * 10m),
        };

        if (!def.HigherIsBetter)
            score = Math.Max(0, 100 - score);

        return (value, score);
    }

    private static void EnsureEditable(EmployeeEvaluation eval)
    {
        if (eval.Status is EmployeeEvaluationStatus.APPROVED or EmployeeEvaluationStatus.IN_REVIEW)
            throw new InvalidOperationException("Approved/in-review evaluations cannot be recalculated. Reject first.");
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

    private static EvaluationPeriodResponse ToPeriod(EvaluationPeriod p) =>
        new(p.Id, p.Name, p.StartDate, p.EndDate, p.Status.ToString(), p.CreatedAt, p.UpdatedAt);

    private static KpiDefinitionResponse ToDef(KpiDefinition d) =>
        new(d.Id, d.Code, d.Name, d.Description, d.Weight, d.HigherIsBetter, d.IsActive);

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
            e.Results.Select(r => new EmployeeKpiResultResponse(
                r.Id, r.KpiDefinitionId, r.KpiDefinition?.Code ?? "", r.KpiDefinition?.Name ?? "",
                r.CalculatedValue, r.AdjustedValue, r.Score, r.Comment)).ToList());
}
