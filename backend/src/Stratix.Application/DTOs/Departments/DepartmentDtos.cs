namespace Stratix.Application.DTOs.Departments;

public record DepartmentResponse(long Id, string Name, string? Description);
public record CreateDepartmentRequest(string Name, string? Description);
public record UpdateDepartmentRequest(string Name, string? Description);
