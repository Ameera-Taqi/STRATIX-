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
                case TaskAssignedEvent a:
                    await NotifyTaskAssignedAsync(a, ct);
                    break;
                case TaskStatusChangedEvent t:
                    await NotifyTaskStatusAsync(t, ct);
                    break;
                case StageCompletedEvent s:
                    await NotifyAsync(new NotificationDraft(
                        s.OrganizationId,
                        s.ProjectManagerId,
                        $"{Actor(s.ActorName)} completed a feature",
                        null,
                        $"/projects/{s.ProjectId}",
                        NotificationType.SUCCESS,
                        s.ActorName,
                        s.ProjectName,
                        "STAGE",
                        s.StageId,
                        s.StageName), ct);
                    break;
                case ProjectCompletedEvent p:
                    await NotifyAsync(new NotificationDraft(
                        p.OrganizationId,
                        p.ProjectManagerId,
                        $"{Actor(p.ActorName)} completed the project",
                        null,
                        $"/projects/{p.ProjectId}",
                        NotificationType.SUCCESS,
                        p.ActorName,
                        p.ProjectName,
                        "PROJECT",
                        p.ProjectId,
                        p.ProjectName), ct);
                    break;
                case RiskRaisedEvent r:
                    await NotifyRiskRaisedAsync(r, ct);
                    break;
                case EvaluationSubmittedEvent ev:
                    await NotifyAsync(new NotificationDraft(
                        ev.OrganizationId,
                        ev.UserId,
                        $"{Actor(ev.ActorName)} submitted your evaluation",
                        "Your performance evaluation was submitted for review.",
                        "/performance",
                        NotificationType.INFO,
                        ev.ActorName,
                        null,
                        "EVALUATION",
                        ev.EvaluationId,
                        null), ct);
                    break;
                case EvaluationApprovedEvent ea:
                    await NotifyAsync(new NotificationDraft(
                        ea.OrganizationId,
                        ea.UserId,
                        $"{Actor(ea.ActorName)} approved your KPI evaluation",
                        "Your performance evaluation was approved.",
                        "/performance",
                        NotificationType.SUCCESS,
                        ea.ActorName,
                        null,
                        "EVALUATION",
                        ea.EvaluationId,
                        null), ct);
                    break;
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private async Task NotifyTaskAssignedAsync(TaskAssignedEvent e, CancellationToken ct)
    {
        if (e.AssigneeId == e.ActorUserId) return;
        await NotifyAsync(new NotificationDraft(
            e.OrganizationId,
            e.AssigneeId,
            $"{Actor(e.ActorName)} assigned you",
            null,
            $"/tasks/{e.TaskId}",
            NotificationType.INFO,
            e.ActorName,
            e.ProjectName,
            "TASK",
            e.TaskId,
            e.TaskTitle), ct);
    }

    private async Task NotifyTaskStatusAsync(TaskStatusChangedEvent e, CancellationToken ct)
    {
        // Avoid inbox noise from routine TODO ↔ IN_PROGRESS moves.
        // Grouping similar rows can come later; for now only surface meaningful transitions.
        if (!IsNotableStatusChange(e.OldStatus, e.NewStatus)) return;

        var recipients = new HashSet<long>();
        if (e.AssigneeId is long a && a != e.ActorUserId) recipients.Add(a);
        if (e.ProjectManagerId is long pm && pm != e.ActorUserId) recipients.Add(pm);

        var title = StatusTitle(e);
        var type = e.NewStatus == DomainTaskStatus.BLOCKED ? NotificationType.WARNING
            : e.NewStatus == DomainTaskStatus.DONE ? NotificationType.SUCCESS
            : NotificationType.INFO;

        foreach (var userId in recipients)
        {
            await NotifyAsync(new NotificationDraft(
                e.OrganizationId,
                userId,
                title,
                $"{e.OldStatus} → {e.NewStatus}",
                $"/tasks/{e.TaskId}",
                type,
                e.ActorName,
                e.ProjectName,
                "TASK",
                e.TaskId,
                e.TaskTitle), ct);
        }
    }

    private async Task NotifyRiskRaisedAsync(RiskRaisedEvent e, CancellationToken ct)
    {
        if (e.RiskLevel is not (RiskLevel.CRITICAL or RiskLevel.HIGH)) return;

        var recipients = new HashSet<long>();
        if (e.OwnerId is long owner && owner != e.ActorUserId) recipients.Add(owner);
        if (e.ProjectManagerId is long pm && pm != e.ActorUserId) recipients.Add(pm);

        var title = e.RiskLevel == RiskLevel.CRITICAL
            ? "Critical risk detected"
            : "High risk detected";
        var type = e.RiskLevel == RiskLevel.CRITICAL ? NotificationType.ERROR : NotificationType.WARNING;

        foreach (var userId in recipients)
        {
            await NotifyAsync(new NotificationDraft(
                e.OrganizationId,
                userId,
                title,
                $"{Actor(e.ActorName)} registered this risk.",
                $"/risks/{e.RiskId}",
                type,
                e.ActorName,
                e.ProjectName,
                "RISK",
                e.RiskId,
                e.RiskTitle), ct);
        }
    }

    private static bool IsNotableStatusChange(DomainTaskStatus from, DomainTaskStatus to) =>
        to is DomainTaskStatus.BLOCKED or DomainTaskStatus.DONE or DomainTaskStatus.REVIEW
        || from == DomainTaskStatus.DONE
        || from == DomainTaskStatus.BLOCKED;

    private static string StatusTitle(TaskStatusChangedEvent e) => e.NewStatus switch
    {
        DomainTaskStatus.BLOCKED => $"{Actor(e.ActorName)} marked a task blocked",
        DomainTaskStatus.DONE => $"{Actor(e.ActorName)} completed a task",
        DomainTaskStatus.REVIEW => $"{Actor(e.ActorName)} submitted a task for review",
        _ when e.OldStatus == DomainTaskStatus.DONE => $"{Actor(e.ActorName)} reopened a task",
        _ when e.OldStatus == DomainTaskStatus.BLOCKED => $"{Actor(e.ActorName)} unblocked a task",
        _ => $"{Actor(e.ActorName)} updated a task",
    };

    private static string Actor(string? name) =>
        string.IsNullOrWhiteSpace(name) ? "Someone" : name.Trim();

    private Task NotifyAsync(NotificationDraft draft, CancellationToken ct)
    {
        if (draft.UserId is null or <= 0) return Task.CompletedTask;
        _db.Add(new Notification
        {
            OrganizationId = draft.OrganizationId,
            UserId = draft.UserId.Value,
            Title = TruncateRequired(draft.Title, 200),
            Message = draft.Message,
            Type = draft.Type,
            Link = draft.Link,
            ActorName = Truncate(draft.ActorName, 200),
            ProjectName = Truncate(draft.ProjectName, 200),
            EntityType = draft.EntityType,
            EntityId = draft.EntityId,
            EntityLabel = Truncate(draft.EntityLabel, 300),
            CreatedAt = DateTimeOffset.UtcNow,
        });
        return Task.CompletedTask;
    }

    private static string TruncateRequired(string value, int max) =>
        string.IsNullOrWhiteSpace(value) ? "Notification"
        : value.Length <= max ? value
        : value[..max];

    private static string? Truncate(string? value, int max) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Length <= max ? value
        : value[..max];

    private sealed record NotificationDraft(
        long OrganizationId,
        long? UserId,
        string Title,
        string? Message,
        string Link,
        NotificationType Type,
        string? ActorName,
        string? ProjectName,
        string? EntityType,
        long? EntityId,
        string? EntityLabel);
}
