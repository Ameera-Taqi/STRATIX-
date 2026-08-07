using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Domain.Workflow;

/// <summary>Allowed task status edges and reason requirements.</summary>
public static class TaskTransitionRules
{
    private static readonly Dictionary<DomainTaskStatus, HashSet<DomainTaskStatus>> Allowed = new()
    {
        [DomainTaskStatus.TODO] = [DomainTaskStatus.IN_PROGRESS, DomainTaskStatus.BLOCKED],
        [DomainTaskStatus.IN_PROGRESS] = [DomainTaskStatus.REVIEW, DomainTaskStatus.BLOCKED, DomainTaskStatus.TODO, DomainTaskStatus.DONE],
        [DomainTaskStatus.REVIEW] = [DomainTaskStatus.DONE, DomainTaskStatus.IN_PROGRESS, DomainTaskStatus.BLOCKED],
        [DomainTaskStatus.DONE] = [DomainTaskStatus.IN_PROGRESS, DomainTaskStatus.TODO],
        [DomainTaskStatus.BLOCKED] = [DomainTaskStatus.TODO, DomainTaskStatus.IN_PROGRESS],
    };

    public static bool CanTransition(DomainTaskStatus from, DomainTaskStatus to) =>
        from == to || (Allowed.TryGetValue(from, out var next) && next.Contains(to));

    public static void EnsureTransition(
        DomainTaskStatus from,
        DomainTaskStatus to,
        string? blockedReason,
        string? reopenReason,
        string? reviewReason)
    {
        if (from == to) return;

        if (!CanTransition(from, to))
            throw new InvalidOperationException($"Transition {from} → {to} is not allowed.");

        if (to == DomainTaskStatus.BLOCKED && string.IsNullOrWhiteSpace(blockedReason))
            throw new ArgumentException("Blocked reason is required when moving a task to BLOCKED.");

        if (from == DomainTaskStatus.DONE && to != DomainTaskStatus.DONE && string.IsNullOrWhiteSpace(reopenReason))
            throw new ArgumentException("Reopen reason is required when reopening a completed task.");

        if (to == DomainTaskStatus.REVIEW && string.IsNullOrWhiteSpace(reviewReason) && from != DomainTaskStatus.REVIEW)
        {
            // Review reason is recommended but optional for first entry from IN_PROGRESS;
            // require it only when returning to review from DONE/BLOCKED paths — keep optional.
        }
    }
}

/// <summary>Stage may close only when all linked tasks are DONE (or there are none).</summary>
public static class StageCloseRules
{
    public static void EnsureCanComplete(IEnumerable<(DomainTaskStatus Status, string Title)> tasks)
    {
        var open = tasks.Where(t => t.Status != DomainTaskStatus.DONE).ToList();
        if (open.Count == 0) return;

        var blocked = open.Count(t => t.Status == DomainTaskStatus.BLOCKED);
        var sample = string.Join(", ", open.Take(3).Select(t => t.Title));
        throw new InvalidOperationException(
            blocked > 0
                ? $"Cannot complete stage: {blocked} blocked task(s) remain ({sample})."
                : $"Cannot complete stage: {open.Count} unfinished task(s) remain ({sample}).");
    }
}

/// <summary>Project may complete only when every stage is DONE (or no stages and all tasks DONE).</summary>
public static class ProjectCloseRules
{
    public static void EnsureCanComplete(
        IEnumerable<StageStatus> stageStatuses,
        IEnumerable<DomainTaskStatus> taskStatuses)
    {
        var stages = stageStatuses.ToList();
        var tasks = taskStatuses.ToList();

        if (stages.Count > 0)
        {
            var unfinished = stages.Count(s => s != StageStatus.DONE);
            if (unfinished > 0)
                throw new InvalidOperationException($"Cannot complete project: {unfinished} stage(s) are not DONE.");
            return;
        }

        var openTasks = tasks.Count(t => t != DomainTaskStatus.DONE);
        if (openTasks > 0)
            throw new InvalidOperationException($"Cannot complete project: {openTasks} unfinished task(s) remain.");
    }
}
