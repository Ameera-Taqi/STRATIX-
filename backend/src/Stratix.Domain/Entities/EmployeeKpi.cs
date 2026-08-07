namespace Stratix.Domain.Entities;

/// <summary>A stored performance snapshot for an employee over a period (e.g. "2026-07").</summary>
public class EmployeeKpi : ITenantScoped, ISoftDeletable, IHasCreatedAt, IHasUpdatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }
    public string Period { get; set; } = string.Empty;
    public int TasksCompleted { get; set; }
    public int TasksOnTime { get; set; }
    public decimal Score { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
