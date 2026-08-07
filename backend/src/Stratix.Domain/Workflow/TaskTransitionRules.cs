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
            throw new InvalidOperationException(DescribeIllegal(from, to));

        if (to == DomainTaskStatus.BLOCKED && string.IsNullOrWhiteSpace(blockedReason))
            throw new ArgumentException("Blocked reason is required when moving a task to BLOCKED.");

        if (from == DomainTaskStatus.DONE && to != DomainTaskStatus.DONE && string.IsNullOrWhiteSpace(reopenReason))
            throw new ArgumentException("Reopen reason is required when reopening a completed task.");

        // Request Changes: reviewer must provide feedback when sending REVIEW → IN_PROGRESS.
        if (from == DomainTaskStatus.REVIEW && to == DomainTaskStatus.IN_PROGRESS
            && string.IsNullOrWhiteSpace(reviewReason))
            throw new ArgumentException("Reason / feedback is required when requesting changes.");
    }

    private static string DescribeIllegal(DomainTaskStatus from, DomainTaskStatus to) =>
        (from, to) switch
        {
            (DomainTaskStatus.TODO, DomainTaskStatus.DONE) =>
                "This task must be started before it can be completed.",
            (DomainTaskStatus.TODO, DomainTaskStatus.REVIEW) =>
                "This task must be started before it can be sent to review.",
            (DomainTaskStatus.BLOCKED, DomainTaskStatus.DONE) or (DomainTaskStatus.BLOCKED, DomainTaskStatus.REVIEW) =>
                "Unblock this task before moving it forward.",
            (DomainTaskStatus.DONE, DomainTaskStatus.BLOCKED) =>
                "Completed tasks cannot be marked blocked. Reopen the task first.",
            (DomainTaskStatus.DONE, DomainTaskStatus.REVIEW) =>
                "Completed tasks cannot move to review. Reopen the task first.",
            (DomainTaskStatus.REVIEW, DomainTaskStatus.TODO) =>
                "Send the task back to In Progress instead of To Do.",
            _ => $"Transition {from} → {to} is not allowed."
        };
}

/// <summary>Feature may close only when all linked tasks are DONE (or there are none).</summary>
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
                ? $"Cannot complete feature: {blocked} blocked task(s) remain ({sample})."
                : $"Cannot complete feature: {open.Count} unfinished task(s) remain ({sample}).");
    }
}

/// <summary>
/// Project may complete when every feature is DONE (or no features and all tasks DONE).
/// Feature order and dates are irrelevant — features may finish in any order / in parallel.
/// </summary>
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
                throw new InvalidOperationException($"Cannot complete project: {unfinished} feature(s) are not DONE.");
            return;
        }

        var openTasks = tasks.Count(t => t != DomainTaskStatus.DONE);
        if (openTasks > 0)
            throw new InvalidOperationException($"Cannot complete project: {openTasks} unfinished task(s) remain.");
    }
}
