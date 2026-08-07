using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Domain.Progress;

/// <summary>
/// Single source of truth for stage/project progress.
/// Stage progress is derived from linked tasks; project progress is effort-weighted.
/// </summary>
public static class ProjectProgressCalculator
{
    public const decimal DefaultEffortHours = 1m;

    public static decimal EffortOf(TaskEffortSlice task) =>
        task.EstimatedHours > 0 ? task.EstimatedHours : DefaultEffortHours;

    public static bool IsDone(DomainTaskStatus status) => status == DomainTaskStatus.DONE;

    /// <summary>Stage progress = done effort / total effort for tasks in the stage.</summary>
    public static decimal StageProgressFromTasks(IEnumerable<TaskEffortSlice> stageTasks)
    {
        var list = stageTasks as IList<TaskEffortSlice> ?? stageTasks.ToList();
        if (list.Count == 0) return 0m;

        decimal total = 0, done = 0;
        foreach (var t in list)
        {
            var effort = EffortOf(t);
            total += effort;
            if (IsDone(t.Status)) done += effort;
        }

        if (total <= 0) return 0m;
        return Round(done / total * 100m);
    }

    /// <summary>Project progress = done effort / total effort across all project tasks.</summary>
    public static ProjectProgressResult ProjectProgressFromEffort(
        IEnumerable<TaskEffortSlice> projectTasks,
        IEnumerable<long>? stageIds = null)
    {
        var tasks = projectTasks as IList<TaskEffortSlice> ?? projectTasks.ToList();
        decimal total = 0, done = 0;
        foreach (var t in tasks)
        {
            var effort = EffortOf(t);
            total += effort;
            if (IsDone(t.Status)) done += effort;
        }

        var progress = total <= 0 ? 0m : Round(done / total * 100m);

        var stages = new List<StageProgressResult>();
        var ids = stageIds?.ToList()
                  ?? tasks.Where(t => t.StageId.HasValue).Select(t => t.StageId!.Value).Distinct().ToList();
        foreach (var stageId in ids)
        {
            var stageTasks = tasks.Where(t => t.StageId == stageId).ToList();
            decimal st = 0, sd = 0;
            foreach (var t in stageTasks)
            {
                var effort = EffortOf(t);
                st += effort;
                if (IsDone(t.Status)) sd += effort;
            }
            stages.Add(new StageProgressResult(
                stageId,
                st <= 0 ? 0m : Round(sd / st * 100m),
                st,
                sd));
        }

        return new ProjectProgressResult(progress, total, done, stages);
    }

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
