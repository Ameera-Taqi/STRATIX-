using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

public sealed record TaskStatusChangedEvent(
    long OrganizationId,
    long ProjectId,
    string ProjectName,
    long TaskId,
    string TaskTitle,
    long? AssigneeId,
    long? ProjectManagerId,
    DomainTaskStatus OldStatus,
    DomainTaskStatus NewStatus,
    long ActorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record StageCompletedEvent(
    long OrganizationId,
    long ProjectId,
    string ProjectName,
    long StageId,
    string StageName,
    long? ProjectManagerId,
    long ActorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ProjectCompletedEvent(
    long OrganizationId,
    long ProjectId,
    string ProjectName,
    long? ProjectManagerId,
    long ActorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record EvaluationSubmittedEvent(
    long OrganizationId,
    long EvaluationId,
    long UserId,
    long PeriodId,
    long ActorUserId,
    DateTimeOffset OccurredAt) : IDomainEvent;
