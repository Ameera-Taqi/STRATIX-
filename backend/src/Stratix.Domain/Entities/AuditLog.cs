using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

public class AuditLog : ITenantScoped, IHasCreatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public AuditAction Action { get; set; }
    public AuditEntityType EntityType { get; set; }
    public long EntityId { get; set; }
    public string? EntityName { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public long? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
