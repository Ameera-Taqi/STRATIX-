using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Domain.Progress;

/// <summary>
/// Single source of truth for stage/project progress.
/// Stage progress is derived from linked tasks; project progress is effort-weighted.
/// Soft-deleted tasks must never be passed in — they are excluded from Progress, on-time,
/// delayed counts, KPI, and health score at the query layer (<c>ActiveOnly</c>).
/// <para>
/// <b>Empty project policy:</b> no tasks → <c>Progress = 0</c>. Never divide by zero and never
/// return an unknown/null progress. For a project still in <c>PLANNED</c>, 0% with no tasks is
/// the normal expected state (not started), not an error.
/// </para>
/// <para>
/// <b>Empty stage policy:</b> a stage with no linked tasks → <c>Progress = 0</c>. Progress is
/// always derived from tasks — not manually editable. Same safe math as projects (no divide-by-zero).
/// </para>
/// <para>
/// <b>Legacy effort policy:</b> missing or non-positive <c>EstimatedHours</c> (null/0 from
/// pre-migration rows) counts as <see cref="DefaultEffortHours"/> (= 1). Progress is never
/// forced to 0 solely because older tasks lack effort estimates — they are treated as equal unit weight.
/// </para>
/// </summary>
public static class ProjectProgressCalculator
{
    /// <summary>Fallback weight when EstimatedHours is null, 0, or negative.</summary>
    public const decimal DefaultEffortHours = 1m;

    /// <summary>Maps stored hours to calculator weight (legacy null/0 → 1).</summary>
    public static decimal EffortOf(TaskEffortSlice task) =>
        task.EstimatedHours > 0 ? task.EstimatedHours : DefaultEffortHours;

    /// <summary>Normalize persisted hours so DB rows match the calculator policy.</summary>
    public static decimal NormalizeStoredHours(decimal estimatedHours) =>
        estimatedHours > 0 ? estimatedHours : DefaultEffortHours;

    public static bool IsDone(DomainTaskStatus status) => status == DomainTaskStatus.DONE;

    /// <summary>
    /// Stage progress = done effort / total effort for tasks in the stage.
    /// Empty stage (no tasks) → 0. Never divide-by-zero; not manually set.
    /// </summary>
    public static decimal StageProgressFromTasks(IEnumerable<TaskEffortSlice> stageTasks)
    {
        var list = stageTasks as IList<TaskEffortSlice> ?? stageTasks.ToList();
        // Explicit empty-stage guard: no tasks → 0%.
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

    /// <summary>
    /// Project progress = done effort / total effort across all project tasks.
    /// Empty task set → 0 (defined result; safe when project is still PLANNED).
    /// </summary>
    public static ProjectProgressResult ProjectProgressFromEffort(
        IEnumerable<TaskEffortSlice> projectTasks,
        IEnumerable<long>? stageIds = null)
    {
        var tasks = projectTasks as IList<TaskEffortSlice> ?? projectTasks.ToList();

        // Explicit empty-set guard: no tasks → 0 (never NaN / never divide-by-zero).
        if (tasks.Count == 0)
        {
            var emptyStageIds = stageIds?.ToList() ?? [];
            return new ProjectProgressResult(
                0m,
                0m,
                0m,
                emptyStageIds.Select(id => new StageProgressResult(id, 0m, 0m, 0m)).ToList());
        }

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
