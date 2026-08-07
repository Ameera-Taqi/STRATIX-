using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

public class ProjectHealthSnapshot : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public long ProjectId { get; set; }
    public Project? Project { get; set; }
    public decimal Score { get; set; }
    public HealthClassification Status { get; set; }
    public decimal Progress { get; set; }
    public int OnTimeTasks { get; set; }
    public int DelayedTasks { get; set; }
    public int CriticalRisks { get; set; }
    public string? NoteKey { get; set; }
    public DateTimeOffset CapturedAt { get; set; }
}
