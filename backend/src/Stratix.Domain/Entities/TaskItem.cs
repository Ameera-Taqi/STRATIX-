using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

public class TaskItem
{
    public long Id { get; set; }
    public long ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public long? StageId { get; set; }
    public ProjectStage? Stage { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Enums.TaskStatus Status { get; set; } = Enums.TaskStatus.TODO;
    public TaskPriority Priority { get; set; } = TaskPriority.MEDIUM;
    public long? AssigneeId { get; set; }
    public User? Assignee { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public decimal Progress { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
