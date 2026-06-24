using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.Audit;

public record AuditLogResponse(long Id, long? UserId, string? UserName, AuditAction Action, AuditEntityType EntityType, long EntityId, string? EntityName, string? OldValues, string? NewValues, string? Description, string? IpAddress, string? UserAgent, long? ProjectId, string? ProjectName, DateTimeOffset CreatedAt);
public record AuditLogPageResponse(IReadOnlyList<AuditLogResponse> Content, long TotalElements, int TotalPages, int Page, int Size);
