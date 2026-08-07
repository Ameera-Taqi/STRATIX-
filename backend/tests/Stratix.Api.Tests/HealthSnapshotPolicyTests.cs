using Stratix.Domain.Enums;
using Stratix.Domain.Workflow;

namespace Stratix.Api.Tests;

public class HealthSnapshotPolicyTests
{
    private static readonly HealthSnapshotPolicy.Fingerprint Base = new(80m, HealthClassification.HEALTHY, 70m, 5, 0, 0, "health.noteStable");

    [Fact]
    public void Force_formal_capture_always_creates()
    {
        var now = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);
        Assert.True(HealthSnapshotPolicy.ShouldCreate(Base, now, Base, now, forceFormalCapture: true));
    }

    [Fact]
    public void First_snapshot_is_created()
    {
        var now = new DateTimeOffset(2026, 8, 7, 12, 0, 0, TimeSpan.Zero);
        Assert.True(HealthSnapshotPolicy.ShouldCreate(null, null, Base, now, forceFormalCapture: false));
    }

    [Fact]
    public void Identical_same_day_is_skipped()
    {
        var captured = new DateTimeOffset(2026, 8, 7, 8, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 8, 7, 18, 0, 0, TimeSpan.Zero);
        Assert.False(HealthSnapshotPolicy.ShouldCreate(Base, captured, Base, now, forceFormalCapture: false));
    }

    [Fact]
    public void Identical_next_utc_day_is_created_once()
    {
        var captured = new DateTimeOffset(2026, 8, 6, 23, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 8, 7, 1, 0, 0, TimeSpan.Zero);
        Assert.True(HealthSnapshotPolicy.ShouldCreate(Base, captured, Base, now, forceFormalCapture: false));
    }

    [Fact]
    public void Meaningful_change_creates_even_same_day()
    {
        var captured = new DateTimeOffset(2026, 8, 7, 8, 0, 0, TimeSpan.Zero);
        var now = new DateTimeOffset(2026, 8, 7, 9, 0, 0, TimeSpan.Zero);
        var changed = Base with { DelayedTasks = 2, Score = 60m, Status = HealthClassification.WARNING };
        Assert.True(HealthSnapshotPolicy.ShouldCreate(Base, captured, changed, now, forceFormalCapture: false));
    }
}
