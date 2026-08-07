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
}

public class ProjectHealthCalculatorTests
{
    [Fact]
    public void Uses_same_formula_as_frontend()
    {
        var factors = new HealthFactors(Progress: 60, OnTimeTasks: 5, DelayedTasks: 2, CriticalRisks: 1);
        var result = ProjectHealthCalculator.Compute(factors);
        Assert.Equal(62m, result.Score);
        Assert.Equal(HealthClassification.WARNING, result.Status);
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
