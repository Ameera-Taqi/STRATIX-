using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Application.DTOs.Tasks;

public record CreateTaskRequest(
    long ProjectId,
    long? StageId,
    string Title,
    string? Description,
    DomainTaskStatus? Status,
    TaskPriority? Priority,
    long? AssigneeId,
    DateOnly? StartDate,
    DateOnly? DueDate,
    decimal? EstimatedHours = null,
    decimal? ActualHours = null);

public record UpdateTaskRequest(
    long ProjectId,
    long? StageId,
    string Title,
    string? Description,
    DomainTaskStatus Status,
    TaskPriority Priority,
    long? AssigneeId,
    DateOnly? StartDate,
    DateOnly? DueDate,
    decimal? EstimatedHours = null,
    decimal? ActualHours = null,
    string? BlockedReason = null,
    string? ReopenReason = null,
    string? ReviewReason = null);

public record UpdateTaskStatusRequest(
    DomainTaskStatus Status,
    string? BlockedReason = null,
    string? ReopenReason = null,
    string? ReviewReason = null);

public record TaskResponse(
    long Id,
    long ProjectId,
    string ProjectName,
    long? StageId,
    string? StageName,
    string Title,
    string Assignee,
    long? AssigneeId,
    string Priority,
    DateOnly? DueDate,
    string Status,
    string? Description,
    decimal EstimatedHours = 1,
    decimal? ActualHours = null,
    string? BlockedReason = null,
    string? ReopenReason = null,
    string? ReviewReason = null);
