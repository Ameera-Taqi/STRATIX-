using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.ChangeRequests;

public record ChangeRequestResponse(long Id, long ProjectId, string ProjectName, string Title, string? Description,
    string Status, string Priority, long RequestedById, string RequestedByName,
    long? ReviewedById, string? ReviewedByName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record CreateChangeRequestRequest(long ProjectId, string Title, string? Description, TaskPriority? Priority, long RequestedById);

public record UpdateChangeRequestRequest(string Title, string? Description, ChangeRequestStatus Status, TaskPriority Priority, long? ReviewedById);
