using Stratix.Domain.Enums;

namespace Stratix.Domain.Workflow;

/// <summary>
/// Health snapshots must not be written on every page read or every trivial recalc.
/// Persist only when factors change, once per UTC day, or on an explicit formal capture.
/// </summary>
public static class HealthSnapshotPolicy
{
    public readonly record struct Fingerprint(
        decimal Score,
        HealthClassification Status,
        decimal Progress,
        int OnTimeTasks,
        int DelayedTasks,
        int CriticalRisks,
        string? NoteKey);

    public static bool ShouldCreate(
        Fingerprint? latest,
        DateTimeOffset? latestCapturedAt,
        Fingerprint current,
        DateTimeOffset nowUtc,
        bool forceFormalCapture)
    {
        if (forceFormalCapture)
            return true;

        if (latest is null || latestCapturedAt is null)
            return true;

        if (!Equals(latest.Value, current))
            return true;

        // One identical snapshot per UTC calendar day keeps trends without table bloat.
        var latestDay = DateOnly.FromDateTime(latestCapturedAt.Value.UtcDateTime);
        var today = DateOnly.FromDateTime(nowUtc.UtcDateTime);
        return latestDay < today;
    }

    private static bool Equals(Fingerprint a, Fingerprint b) =>
        a.Score == b.Score &&
        a.Status == b.Status &&
        a.Progress == b.Progress &&
        a.OnTimeTasks == b.OnTimeTasks &&
        a.DelayedTasks == b.DelayedTasks &&
        a.CriticalRisks == b.CriticalRisks &&
        string.Equals(a.NoteKey, b.NoteKey, StringComparison.Ordinal);
}
