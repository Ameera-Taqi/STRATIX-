namespace Stratix.Application.DTOs.Roles;

public record OrganizationRoleResponse(
    long Id,
    string Code,
    string Name,
    string? Description,
    string BaseRole,
    bool IsSystem,
    DateTimeOffset CreatedAt);

public record CreateOrganizationRoleRequest(
    string Name,
    string? Code,
    string? Description,
    string BaseRole);

public record UpdateOrganizationRoleRequest(
    string Name,
    string? Description,
    string BaseRole);
