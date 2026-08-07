using Stratix.Domain.Enums;
using Stratix.Domain.Workflow;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Domain.Progress;

/// <summary>
/// Raw diagnostic counts (for snapshots / UI). Scoring uses <see cref="HealthNormalizedRates"/>.
/// </summary>
public readonly record struct HealthFactors(
    decimal Progress,
    int OnTimeTasks,
    int DelayedTasks,
    int CriticalRisks,
    int TotalTasks = 0,
    int CompletedTasks = 0,
    int BlockedTasks = 0);

/// <summary>Every component is on a known 0–100 (or capped penalty) scale — never raw task/risk counts.</summary>
public readonly record struct HealthNormalizedRates(
    decimal Progress,
    decimal OnTimeCompletionRate,
    decimal DelayedTaskRate,
    decimal BlockedWorkRate,
    decimal CriticalRiskPenalty);

public readonly record struct HealthScoreResult(
    decimal Score,
    HealthClassification Status,
    string NoteKey,
    HealthFactors Factors,
    HealthNormalizedRates Rates);

/// <summary>
/// Size-normalized project health:
/// <c>score = clamp(0.35·Progress + 0.35·OnTimeRate + 0.15·(100−DelayedRate) + 0.15·(100−BlockedRate) − CriticalPenalty, 0..100)</c>.
/// Soft-deleted tasks must not be included. Reopened tasks do not count as on-time completions.
/// </summary>
public static class ProjectHealthCalculator
{
    public const decimal ProgressWeight = 0.35m;
    public const decimal OnTimeWeight = 0.35m;
    public const decimal DelayedWeight = 0.15m;
    public const decimal BlockedWeight = 0.15m;

    /// <summary>Points deducted per open critical risk.</summary>
    public const decimal CriticalRiskPointsEach = 15m;

    /// <summary>Cap so risk alone cannot dominate beyond a known band.</summary>
    public const decimal CriticalRiskPenaltyCap = 45m;

    public static HealthNormalizedRates Normalize(HealthFactors factors)
    {
        var progress = Clamp(factors.Progress, 0, 100);
        var onTimeRate = factors.CompletedTasks <= 0
            ? 100m
            : Clamp(RoundPct(factors.OnTimeTasks * 100m / factors.CompletedTasks), 0, 100);
        var delayedRate = factors.TotalTasks <= 0
            ? 0m
            : Clamp(RoundPct(factors.DelayedTasks * 100m / factors.TotalTasks), 0, 100);
        var blockedRate = factors.TotalTasks <= 0
            ? 0m
            : Clamp(RoundPct(factors.BlockedTasks * 100m / factors.TotalTasks), 0, 100);
        var criticalPenalty = Clamp(factors.CriticalRisks * CriticalRiskPointsEach, 0, CriticalRiskPenaltyCap);

        return new HealthNormalizedRates(progress, onTimeRate, delayedRate, blockedRate, criticalPenalty);
    }

    public static HealthScoreResult Compute(HealthFactors factors)
    {
        // Empty project: progress only — avoid inventing a healthy score from neutral rates.
        if (factors.TotalTasks <= 0)
        {
            var emptyProgress = Clamp(factors.Progress, 0, 100);
            var emptyRates = new HealthNormalizedRates(emptyProgress, 100m, 0m, 0m, 0m);
            var emptyScore = RoundPct(emptyProgress);
            return new HealthScoreResult(
                emptyScore,
                StatusFromScore(emptyScore, factors),
                "health.noteNoTasks",
                factors,
                emptyRates);
        }

        var rates = Normalize(factors);
        var score = Clamp(
            RoundPct(
                rates.Progress * ProgressWeight
                + rates.OnTimeCompletionRate * OnTimeWeight
                + (100m - rates.DelayedTaskRate) * DelayedWeight
                + (100m - rates.BlockedWorkRate) * BlockedWeight
                - rates.CriticalRiskPenalty),
            0,
            100);

        return new HealthScoreResult(
            score,
            StatusFromScore(score, factors),
            NoteKeyFromFactors(factors),
            factors,
            rates);
    }

    public static HealthFactors BuildFactors(
        decimal progress,
        IEnumerable<TaskEffortSlice> tasks,
        int criticalOpenRisks,
        DateOnly today)
    {
        var list = tasks as IList<TaskEffortSlice> ?? tasks.ToList();

        var completed = list.Where(t => TaskCompletionPolicy.CountsAsCompleted(t.Status, t.CompletedAt)).ToList();
        var onTime = completed.Count(t =>
            t.DueDate is null ||
            (t.CompletedAt is { } c && DateOnly.FromDateTime(c.UtcDateTime) <= t.DueDate));

        var delayed = list.Count(t =>
            t.DueDate is { } due &&
            due < today &&
            t.Status != DomainTaskStatus.DONE);

        var blocked = list.Count(t => t.Status == DomainTaskStatus.BLOCKED);

        return new HealthFactors(
            progress,
            onTime,
            delayed,
            criticalOpenRisks,
            list.Count,
            completed.Count,
            blocked);
    }

    private static HealthClassification StatusFromScore(decimal score, HealthFactors factors)
    {
        if (score >= 70) return HealthClassification.HEALTHY;
        if (score >= 45) return HealthClassification.WARNING;
        var hasTrouble = factors.DelayedTasks > 0 || factors.CriticalRisks > 0 || factors.BlockedTasks > 0;
        return hasTrouble ? HealthClassification.CRITICAL : HealthClassification.WARNING;
    }

    private static string NoteKeyFromFactors(HealthFactors factors)
    {
        if (factors.CriticalRisks > 0) return "health.noteRisks";
        if (factors.DelayedTasks > 0) return "health.noteOverdueTasks";
        if (factors.BlockedTasks > 0) return "health.noteBlocked";
        if (factors.TotalTasks == 0) return "health.noteNoTasks";
        return "health.noteStable";
    }

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        Math.Min(max, Math.Max(min, value));

    private static decimal RoundPct(decimal value) =>
        Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
