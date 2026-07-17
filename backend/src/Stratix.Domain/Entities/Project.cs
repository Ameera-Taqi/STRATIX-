using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

public class Project : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.PLANNED;
    public ProjectPriority Priority { get; set; } = ProjectPriority.MEDIUM;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Progress { get; set; }
    public long? ProjectManagerId { get; set; }
    public User? ProjectManager { get; set; }
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public ICollection<ProjectStage> Stages { get; set; } = new List<ProjectStage>();
    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<ProjectRisk> Risks { get; set; } = new List<ProjectRisk>();
}
