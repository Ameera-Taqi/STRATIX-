namespace Stratix.Domain.Entities;

/// <summary>A comment left by a user on a task.</summary>
public class TaskComment : ITenantScoped, ISoftDeletable, IHasCreatedAt
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long TaskId { get; set; }
    public TaskItem? Task { get; set; }
    public long UserId { get; set; }
    public User? User { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
