using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.Audit;

public record AuditLogResponse(long Id, long? UserId, string? UserName, AuditAction Action, AuditEntityType EntityType, long EntityId, string? EntityName, string? OldValues, string? NewValues, string? Description, string? IpAddress, string? UserAgent, long? ProjectId, string? ProjectName, DateTimeOffset CreatedAt);

/// <summary>Paged audit response — same 1-based contract as <c>PagedResponse&lt;T&gt;</c>, with legacy aliases.</summary>
public record AuditLogPageResponse(
    IReadOnlyList<AuditLogResponse> Items,
    int Total,
    int TotalPages,
    int Page,
    int PageSize)
{
    // Legacy JSON names used by the Angular client before pagination unification.
    public IReadOnlyList<AuditLogResponse> Content => Items;
    public long TotalElements => Total;
    public int Size => PageSize;
}
