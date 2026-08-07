namespace Stratix.Application.DTOs.Health;

public record ProjectHealthSnapshotResponse(
    long Id,
    long ProjectId,
    string ProjectName,
    decimal Score,
    string Status,
    decimal Progress,
    int OnTimeTasks,
    int DelayedTasks,
    int CriticalRisks,
    string? NoteKey,
    DateTimeOffset CapturedAt);
