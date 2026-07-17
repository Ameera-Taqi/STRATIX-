using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.Milestones;

public record MilestoneResponse(long Id, long ProjectId, string ProjectName, string Title,
    DateOnly DueDate, DateOnly? CompletedDate, string Status, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record CreateMilestoneRequest(long ProjectId, string Title, DateOnly DueDate, DateOnly? CompletedDate, MilestoneStatus? Status);

public record UpdateMilestoneRequest(long ProjectId, string Title, DateOnly DueDate, DateOnly? CompletedDate, MilestoneStatus Status);
