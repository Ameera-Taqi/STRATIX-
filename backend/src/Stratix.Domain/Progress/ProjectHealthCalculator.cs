using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Domain.Progress;

public readonly record struct HealthFactors(
    decimal Progress,
    int OnTimeTasks,
    int DelayedTasks,
    int CriticalRisks);

public readonly record struct HealthScoreResult(
    decimal Score,
    HealthClassification Status,
    string NoteKey,
    HealthFactors Factors);

/// <summary>
/// Unified health formula shared by API, dashboard, and snapshots:
/// score = clamp(progress + onTime − delayed − criticalRisks, 0..100).
/// </summary>
public static class ProjectHealthCalculator
{
    public static HealthScoreResult Compute(HealthFactors factors)
    {
        var score = Clamp(factors.Progress + factors.OnTimeTasks - factors.DelayedTasks - factors.CriticalRisks, 0, 100);
        var status = StatusFromScore(score, factors);
        return new HealthScoreResult(score, status, NoteKeyFromFactors(factors), factors);
    }

    public static HealthFactors BuildFactors(
        decimal progress,
        IEnumerable<TaskEffortSlice> tasks,
        int criticalOpenRisks,
        DateOnly today)
    {
        var list = tasks as IList<TaskEffortSlice> ?? tasks.ToList();

        var onTime = list.Count(t =>
            t.Status == DomainTaskStatus.DONE &&
            (t.DueDate is null ||
             (t.CompletedAt is { } c && DateOnly.FromDateTime(c.UtcDateTime) <= t.DueDate)));

        var delayed = list.Count(t =>
            t.DueDate is { } due &&
            due < today &&
            t.Status != DomainTaskStatus.DONE);

        return new HealthFactors(progress, onTime, delayed, criticalOpenRisks);
    }

    private static HealthClassification StatusFromScore(decimal score, HealthFactors factors)
    {
        if (score >= 70) return HealthClassification.HEALTHY;
        if (score >= 45) return HealthClassification.WARNING;
        var hasTrouble = factors.DelayedTasks > 0 || factors.CriticalRisks > 0;
        return hasTrouble ? HealthClassification.CRITICAL : HealthClassification.WARNING;
    }

    private static string NoteKeyFromFactors(HealthFactors factors)
    {
        if (factors.CriticalRisks > 0) return "health.noteRisks";
        if (factors.DelayedTasks > 0) return "health.noteOverdueTasks";
        if (factors.OnTimeTasks == 0 && factors.DelayedTasks == 0) return "health.noteNoTasks";
        return "health.noteStable";
    }

    private static decimal Clamp(decimal value, decimal min, decimal max) =>
        Math.Min(max, Math.Max(min, value));
}
