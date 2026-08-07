using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;

namespace Stratix.Domain.Progress;

/// <summary>Minimal task shape for progress / health calculators.</summary>
public readonly record struct TaskEffortSlice(
    long Id,
    long? StageId,
    DomainTaskStatus Status,
    decimal EstimatedHours,
    DateOnly? DueDate,
    DateTimeOffset? CompletedAt);

public readonly record struct StageProgressResult(long StageId, decimal Progress, decimal EffortTotal, decimal EffortDone);

public readonly record struct ProjectProgressResult(
    decimal Progress,
    decimal EffortTotal,
    decimal EffortDone,
    IReadOnlyList<StageProgressResult> Stages);
