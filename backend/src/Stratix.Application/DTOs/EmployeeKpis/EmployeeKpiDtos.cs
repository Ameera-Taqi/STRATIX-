namespace Stratix.Application.DTOs.EmployeeKpis;

public record EmployeeKpiResponse(long Id, long UserId, string UserName, string Period,
    int TasksCompleted, int TasksOnTime, decimal Score, string? Notes, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

public record CreateEmployeeKpiRequest(long UserId, string Period, int TasksCompleted, int TasksOnTime, decimal Score, string? Notes);

public record UpdateEmployeeKpiRequest(string Period, int TasksCompleted, int TasksOnTime, decimal Score, string? Notes);
