using Stratix.Domain.Entities;

namespace Stratix.Application.Common;

/// <summary>
/// Soft-delete exclusion for metrics. Soft-deleted tasks must never enter:
/// Progress, on-time rate, delayed tasks, KPI calculations, or health score.
/// Global EF query filters already hide <c>IsDeleted</c>; this makes the rule explicit
/// at call sites that build those metrics.
/// </summary>
public static class ActiveTaskQuery
{
    public static IQueryable<TaskItem> ActiveOnly(this IQueryable<TaskItem> tasks) =>
        tasks.Where(t => !t.IsDeleted);
}
