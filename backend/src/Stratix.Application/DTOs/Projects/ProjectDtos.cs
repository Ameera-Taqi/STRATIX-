using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.Projects;

public record CreateProjectRequest(string Name, string? Description, long? DepartmentId, long? ProjectManagerId, DateOnly? StartDate, DateOnly? EndDate, ProjectStatus? Status, ProjectPriority? Priority, decimal? Progress);
public record UpdateProjectRequest(string Name, string? Description, long? DepartmentId, long? ProjectManagerId, DateOnly? StartDate, DateOnly? EndDate, ProjectStatus Status, ProjectPriority Priority, decimal? Progress);
public record ProjectResponse(long Id, string Name, string Department, string Manager, long? ManagerId, DateOnly? StartDate, DateOnly? EndDate, int Progress, string Status, string Priority, string Owner, DateOnly? Deadline);
