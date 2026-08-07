using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.Stages;

public record CreateStageRequest(string Name, string? Description, DateOnly? StartDate, DateOnly? EndDate, StageStatus? Status, decimal? Progress, int? OrderNumber);
public record UpdateStageRequest(string Name, string? Description, DateOnly? StartDate, DateOnly? EndDate, StageStatus Status, decimal? Progress, int OrderNumber);
public record StageResponse(long Id, long ProjectId, string Name, string? Description, DateOnly? StartDate, DateOnly? EndDate, decimal Progress, string Status, int OrderNumber);
public record MoveStageRequest(string Direction);
