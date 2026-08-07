using Stratix.Domain.Enums;

namespace Stratix.Application.DTOs.Users;

public record CreateUserRequest(string Name, string Email, string Password, UserRole Role, string? JobTitle, UserStatus? Status, long? DepartmentId);
public record UpdateUserRequest(string Name, string Email, UserRole Role, string? JobTitle, UserStatus Status, long? DepartmentId);
public record UserResponse(long Id, string Name, string Email, UserRole Role, string? JobTitle, UserStatus Status, long? DepartmentId, string? DepartmentName, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);

/// <summary>Lightweight tenant directory entry for assignee/manager pickers (not admin CRUD).</summary>
public record UserDirectoryItem(
    long Id,
    string Name,
    string Email,
    UserRole Role,
    string? JobTitle,
    UserStatus Status,
    long? DepartmentId,
    string? DepartmentName);
