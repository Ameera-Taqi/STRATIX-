using Stratix.Domain.Enums;
using Stratix.Domain.Progress;
using Stratix.Domain.Workflow;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Api.Tests;

public class ProjectProgressCalculatorTests
{
    [Fact]
    public void Stage_progress_is_effort_weighted()
    {
        var tasks = new[]
        {
            new TaskEffortSlice(1, 10, DomainTaskStatus.DONE, 3, null, DateTimeOffset.UtcNow),
            new TaskEffortSlice(2, 10, DomainTaskStatus.TODO, 1, null, null),
        };

        var progress = ProjectProgressCalculator.StageProgressFromTasks(tasks);
        Assert.Equal(75m, progress);
    }

    [Fact]
    public void Project_progress_aggregates_effort_across_stages()
    {
        var tasks = new[]
        {
            new TaskEffortSlice(1, 1, DomainTaskStatus.DONE, 2, null, DateTimeOffset.UtcNow),
            new TaskEffortSlice(2, 2, DomainTaskStatus.DONE, 2, null, DateTimeOffset.UtcNow),
            new TaskEffortSlice(3, 2, DomainTaskStatus.IN_PROGRESS, 4, null, null),
        };

        var result = ProjectProgressCalculator.ProjectProgressFromEffort(tasks, [1, 2]);
        Assert.Equal(50m, result.Progress);
        Assert.Equal(100m, result.Stages.First(s => s.StageId == 1).Progress);
        Assert.Equal(33.33m, result.Stages.First(s => s.StageId == 2).Progress);
    }

    [Fact]
    public void No_tasks_yields_zero_progress_not_unknown()
    {
        var result = ProjectProgressCalculator.ProjectProgressFromEffort([], [10, 20]);
        Assert.Equal(0m, result.Progress);
        Assert.Equal(0m, result.EffortTotal);
        Assert.Equal(0m, result.EffortDone);
        Assert.All(result.Stages, s => Assert.Equal(0m, s.Progress));
    }

    [Fact]
    public void Empty_stage_yields_zero_progress()
    {
        Assert.Equal(0m, ProjectProgressCalculator.StageProgressFromTasks([]));

        var result = ProjectProgressCalculator.ProjectProgressFromEffort(
            [new TaskEffortSlice(1, 1, DomainTaskStatus.DONE, 2, null, DateTimeOffset.UtcNow)],
            [1, 2]); // stage 2 has no tasks

        Assert.Equal(100m, result.Stages.First(s => s.StageId == 1).Progress);
        Assert.Equal(0m, result.Stages.First(s => s.StageId == 2).Progress);
    }

    [Fact]
    public void Moving_task_between_stages_updates_source_destination_and_project()
    {
        // Before: Stage A has DONE task; Stage B empty → A=100, B=0, project=100
        var before = ProjectProgressCalculator.ProjectProgressFromEffort(
            [new TaskEffortSlice(1, 1, DomainTaskStatus.DONE, 1, null, DateTimeOffset.UtcNow)],
            [1, 2]);
        Assert.Equal(100m, before.Stages.First(s => s.StageId == 1).Progress);
        Assert.Equal(0m, before.Stages.First(s => s.StageId == 2).Progress);
        Assert.Equal(100m, before.Progress);

        // After move Stage A → Stage B: A empty → 0; B has DONE → 100; project still 100
        var after = ProjectProgressCalculator.ProjectProgressFromEffort(
            [new TaskEffortSlice(1, 2, DomainTaskStatus.DONE, 1, null, DateTimeOffset.UtcNow)],
            [1, 2]);
        Assert.Equal(0m, after.Stages.First(s => s.StageId == 1).Progress);
        Assert.Equal(100m, after.Stages.First(s => s.StageId == 2).Progress);
        Assert.Equal(100m, after.Progress);

        // Move incomplete task into B that already has a DONE sibling → B and project change
        var mixed = ProjectProgressCalculator.ProjectProgressFromEffort(
            [
                new TaskEffortSlice(1, 2, DomainTaskStatus.DONE, 1, null, DateTimeOffset.UtcNow),
                new TaskEffortSlice(2, 2, DomainTaskStatus.TODO, 1, null, null),
            ],
            [1, 2]);
        Assert.Equal(0m, mixed.Stages.First(s => s.StageId == 1).Progress);
        Assert.Equal(50m, mixed.Stages.First(s => s.StageId == 2).Progress);
        Assert.Equal(50m, mixed.Progress);
    }
}

public class ProjectHealthCalculatorTests
{
    [Fact]
    public void Perfect_normalized_rates_yield_100()
    {
        var result = ProjectHealthCalculator.Compute(new HealthFactors(
            Progress: 100, OnTimeTasks: 10, DelayedTasks: 0, CriticalRisks: 0,
            TotalTasks: 10, CompletedTasks: 10, BlockedTasks: 0));
        Assert.Equal(100m, result.Score);
        Assert.Equal(HealthClassification.HEALTHY, result.Status);
        Assert.Equal(100m, result.Rates.OnTimeCompletionRate);
        Assert.Equal(0m, result.Rates.DelayedTaskRate);
        Assert.Equal(0m, result.Rates.BlockedWorkRate);
        Assert.Equal(0m, result.Rates.CriticalRiskPenalty);
    }

    [Fact]
    public void Critical_risk_uses_capped_penalty_not_raw_count()
    {
        var one = ProjectHealthCalculator.Compute(new HealthFactors(
            100, 10, 0, CriticalRisks: 1, TotalTasks: 10, CompletedTasks: 10, BlockedTasks: 0));
        Assert.Equal(15m, one.Rates.CriticalRiskPenalty);
        Assert.Equal(85m, one.Score);

        var many = ProjectHealthCalculator.Compute(new HealthFactors(
            100, 10, 0, CriticalRisks: 10, TotalTasks: 10, CompletedTasks: 10, BlockedTasks: 0));
        Assert.Equal(45m, many.Rates.CriticalRiskPenalty); // capped
        Assert.Equal(55m, many.Score);
    }

    [Fact]
    public void Same_delayed_count_hurts_small_project_more_than_large()
    {
        var small = ProjectHealthCalculator.Compute(new HealthFactors(
            Progress: 80, OnTimeTasks: 4, DelayedTasks: 5, CriticalRisks: 0,
            TotalTasks: 10, CompletedTasks: 4, BlockedTasks: 0));
        var large = ProjectHealthCalculator.Compute(new HealthFactors(
            Progress: 80, OnTimeTasks: 400, DelayedTasks: 5, CriticalRisks: 0,
            TotalTasks: 500, CompletedTasks: 400, BlockedTasks: 0));

        Assert.Equal(50m, small.Rates.DelayedTaskRate);
        Assert.Equal(1m, large.Rates.DelayedTaskRate);
        Assert.True(small.Score < large.Score);
    }

    [Fact]
    public void Empty_project_uses_progress_only()
    {
        var result = ProjectHealthCalculator.Compute(new HealthFactors(
            Progress: 40, OnTimeTasks: 0, DelayedTasks: 0, CriticalRisks: 0,
            TotalTasks: 0, CompletedTasks: 0, BlockedTasks: 0));
        Assert.Equal(40m, result.Score);
        Assert.Equal("health.noteNoTasks", result.NoteKey);
    }

    [Fact]
    public void BuildFactors_includes_blocked_rate_inputs()
    {
        var today = new DateOnly(2026, 8, 7);
        var factors = ProjectHealthCalculator.BuildFactors(
            50m,
            [
                new TaskEffortSlice(1, 1, DomainTaskStatus.DONE, 1, today, today.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)),
                new TaskEffortSlice(2, 1, DomainTaskStatus.BLOCKED, 1, today.AddDays(1), null),
                new TaskEffortSlice(3, 1, DomainTaskStatus.TODO, 1, today.AddDays(-1), null),
            ],
            criticalOpenRisks: 0,
            today);

        Assert.Equal(3, factors.TotalTasks);
        Assert.Equal(1, factors.CompletedTasks);
        Assert.Equal(1, factors.OnTimeTasks);
        Assert.Equal(1, factors.BlockedTasks);
        Assert.Equal(1, factors.DelayedTasks);

        var score = ProjectHealthCalculator.Compute(factors);
        Assert.Equal(RoundExpected(factors), score.Score);
    }

    private static decimal RoundExpected(HealthFactors f)
    {
        var r = ProjectHealthCalculator.Normalize(f);
        return Math.Round(
            r.Progress * ProjectHealthCalculator.ProgressWeight
            + r.OnTimeCompletionRate * ProjectHealthCalculator.OnTimeWeight
            + (100m - r.DelayedTaskRate) * ProjectHealthCalculator.DelayedWeight
            + (100m - r.BlockedWorkRate) * ProjectHealthCalculator.BlockedWeight
            - r.CriticalRiskPenalty,
            1,
            MidpointRounding.AwayFromZero);
    }
}

public class TaskTransitionRulesTests
{
    [Fact]
    public void Requires_blocked_reason()
    {
        Assert.Throws<ArgumentException>(() =>
            TaskTransitionRules.EnsureTransition(DomainTaskStatus.TODO, DomainTaskStatus.BLOCKED, null, null, null));
    }

    [Fact]
    public void Requires_reopen_reason_from_done()
    {
        Assert.Throws<ArgumentException>(() =>
            TaskTransitionRules.EnsureTransition(DomainTaskStatus.DONE, DomainTaskStatus.IN_PROGRESS, null, null, null));
    }

    [Fact]
    public void Rejects_illegal_edge()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TaskTransitionRules.EnsureTransition(DomainTaskStatus.TODO, DomainTaskStatus.DONE, null, null, null));
    }
}

public class StageCloseRulesTests
{
    [Fact]
    public void Blocks_complete_when_open_tasks_remain()
    {
        Assert.Throws<InvalidOperationException>(() =>
            StageCloseRules.EnsureCanComplete([
                (DomainTaskStatus.DONE, "A"),
                (DomainTaskStatus.IN_PROGRESS, "B"),
            ]));
    }
}

public class TaskCompletionPolicyTests
{
    [Fact]
    public void Reopening_clears_CompletedAt_and_stops_counting_as_completed()
    {
        var task = new Stratix.Domain.Entities.TaskItem
        {
            Status = DomainTaskStatus.DONE,
            CompletedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Progress = 100,
        };

        Assert.True(TaskCompletionPolicy.CountsAsCompleted(task));

        task.Status = DomainTaskStatus.IN_PROGRESS;
        TaskCompletionPolicy.ApplyStatus(task, DomainTaskStatus.IN_PROGRESS);

        Assert.Null(task.CompletedAt);
        Assert.Equal(0, task.Progress);
        Assert.False(TaskCompletionPolicy.CountsAsCompleted(task));
    }

    [Fact]
    public void Reopened_task_reduces_project_progress_and_on_time_health()
    {
        var completedAt = DateTimeOffset.UtcNow;
        var before = ProjectProgressCalculator.ProjectProgressFromEffort(
            [new TaskEffortSlice(1, 1, DomainTaskStatus.DONE, 1, null, completedAt)],
            [1]);
        Assert.Equal(100m, before.Progress);

        // After reopen: status IN_PROGRESS, CompletedAt cleared → 0% progress.
        var after = ProjectProgressCalculator.ProjectProgressFromEffort(
            [new TaskEffortSlice(1, 1, DomainTaskStatus.IN_PROGRESS, 1, null, null)],
            [1]);
        Assert.Equal(0m, after.Progress);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var healthBefore = ProjectHealthCalculator.BuildFactors(
            100m,
            [new TaskEffortSlice(1, 1, DomainTaskStatus.DONE, 1, today, completedAt)],
            0,
            today);
        Assert.Equal(1, healthBefore.OnTimeTasks);

        var healthAfter = ProjectHealthCalculator.BuildFactors(
            0m,
            [new TaskEffortSlice(1, 1, DomainTaskStatus.IN_PROGRESS, 1, today, null)],
            0,
            today);
        Assert.Equal(0, healthAfter.OnTimeTasks);
    }
}

public class LegacyEstimatedHoursPolicyTests
{
    [Fact]
    public void Missing_or_zero_estimated_hours_use_unit_weight_not_zero_progress()
    {
        var tasks = new[]
        {
            new TaskEffortSlice(1, null, DomainTaskStatus.DONE, 0, null, DateTimeOffset.UtcNow),
            new TaskEffortSlice(2, null, DomainTaskStatus.TODO, 0, null, null),
        };

        Assert.Equal(1m, ProjectProgressCalculator.EffortOf(tasks[0]));
        Assert.Equal(1m, ProjectProgressCalculator.NormalizeStoredHours(0));
        Assert.Equal(1m, ProjectProgressCalculator.NormalizeStoredHours(-5));

        var result = ProjectProgressCalculator.ProjectProgressFromEffort(tasks);
        Assert.Equal(50m, result.Progress);
        Assert.Equal(2m, result.EffortTotal);
        Assert.Equal(1m, result.EffortDone);
    }
}
