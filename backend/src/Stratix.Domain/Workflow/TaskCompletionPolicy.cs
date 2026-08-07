using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Domain.Workflow;

/// <summary>
/// Completion / reopen policy for tasks.
/// <para>
/// <b>Complete (→ DONE):</b> stamp <c>CompletedAt</c> (keep existing if already set), progress = 100.
/// </para>
/// <para>
/// <b>Reopen (DONE → IN_PROGRESS / TODO):</b> clear <c>CompletedAt</c>. The task must not count as
/// completed in Progress, Health (on-time), or KPI until completed again.
/// </para>
/// </summary>
public static class TaskCompletionPolicy
{
    public static void ApplyStatus(TaskItem task, DomainTaskStatus newStatus)
    {
        if (newStatus == DomainTaskStatus.DONE)
        {
            task.CompletedAt ??= DateTimeOffset.UtcNow;
            task.Progress = 100;
            return;
        }

        // Leaving DONE (or any non-DONE status): clear completion stamp.
        task.CompletedAt = null;
        if (task.Progress >= 100)
            task.Progress = 0;
    }

    /// <summary>KPI / on-time: only DONE with a completion timestamp counts as completed.</summary>
    public static bool CountsAsCompleted(DomainTaskStatus status, DateTimeOffset? completedAt) =>
        status == DomainTaskStatus.DONE && completedAt.HasValue;

    public static bool CountsAsCompleted(TaskItem task) =>
        CountsAsCompleted(task.Status, task.CompletedAt);
}
