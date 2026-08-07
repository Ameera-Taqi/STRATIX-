using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using Stratix.Domain.Events;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Application.Services;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default);
    Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default);
}

/// <summary>In-process dispatcher that turns domain events into in-app notifications.</summary>
public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IApplicationDbContext _db;

    public DomainEventDispatcher(IApplicationDbContext db) => _db = db;

    public Task DispatchAsync(IDomainEvent domainEvent, CancellationToken ct = default) =>
        DispatchAsync([domainEvent], ct);

    public async Task DispatchAsync(IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        foreach (var e in events)
        {
            switch (e)
            {
                case TaskStatusChangedEvent t:
                    await NotifyTaskStatusAsync(t, ct);
                    break;
                case StageCompletedEvent s:
                    await NotifyAsync(s.OrganizationId, s.ProjectManagerId, "Stage completed",
                        $"Stage \"{s.StageName}\" on {s.ProjectName} is complete.",
                        $"/projects/{s.ProjectId}", NotificationType.SUCCESS, ct);
                    break;
                case ProjectCompletedEvent p:
                    await NotifyAsync(p.OrganizationId, p.ProjectManagerId, "Project completed",
                        $"Project \"{p.ProjectName}\" was marked completed.",
                        $"/projects/{p.ProjectId}", NotificationType.SUCCESS, ct);
                    break;
                case EvaluationSubmittedEvent ev:
                    // Notify org admins is out of band; notify the employee.
                    await NotifyAsync(ev.OrganizationId, ev.UserId, "Evaluation submitted",
                        "Your performance evaluation was submitted for review.",
                        "/performance", NotificationType.INFO, ct);
                    break;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task NotifyTaskStatusAsync(TaskStatusChangedEvent e, CancellationToken ct)
    {
        var recipients = new HashSet<long>();
        if (e.AssigneeId is long a && a != e.ActorUserId) recipients.Add(a);
        if (e.ProjectManagerId is long pm && pm != e.ActorUserId) recipients.Add(pm);

        foreach (var userId in recipients)
        {
            await NotifyAsync(
                e.OrganizationId,
                userId,
                "Task status updated",
                $"\"{e.TaskTitle}\" moved {e.OldStatus} → {e.NewStatus}.",
                $"/tasks/{e.TaskId}",
                e.NewStatus == DomainTaskStatus.BLOCKED ? NotificationType.WARNING : NotificationType.INFO,
                ct);
        }
    }

    private Task NotifyAsync(
        long orgId,
        long? userId,
        string title,
        string message,
        string link,
        NotificationType type,
        CancellationToken ct)
    {
        if (userId is null or <= 0) return Task.CompletedTask;
        _db.Add(new Notification
        {
            OrganizationId = orgId,
            UserId = userId.Value,
            Title = title,
            Message = message,
            Type = type,
            Link = link,
            CreatedAt = DateTimeOffset.UtcNow,
        });
        return Task.CompletedTask;
    }
}
