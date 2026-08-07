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
    string ActorName,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record TaskAssignedEvent(
    long OrganizationId,
    long ProjectId,
    string ProjectName,
    long TaskId,
    string TaskTitle,
    long AssigneeId,
    long ActorUserId,
    string ActorName,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record StageCompletedEvent(
    long OrganizationId,
    long ProjectId,
    string ProjectName,
    long StageId,
    string StageName,
    long? ProjectManagerId,
    long ActorUserId,
    string ActorName,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record ProjectCompletedEvent(
    long OrganizationId,
    long ProjectId,
    string ProjectName,
    long? ProjectManagerId,
    long ActorUserId,
    string ActorName,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record RiskRaisedEvent(
    long OrganizationId,
    long ProjectId,
    string ProjectName,
    long RiskId,
    string RiskTitle,
    RiskLevel RiskLevel,
    long? OwnerId,
    long? ProjectManagerId,
    long ActorUserId,
    string ActorName,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record EvaluationSubmittedEvent(
    long OrganizationId,
    long EvaluationId,
    long UserId,
    long PeriodId,
    long ActorUserId,
    string ActorName,
    DateTimeOffset OccurredAt) : IDomainEvent;

public sealed record EvaluationApprovedEvent(
    long OrganizationId,
    long EvaluationId,
    long UserId,
    long PeriodId,
    long ActorUserId,
    string ActorName,
    DateTimeOffset OccurredAt) : IDomainEvent;
