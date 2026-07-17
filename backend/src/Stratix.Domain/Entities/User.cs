using Stratix.Domain.Enums;

namespace Stratix.Domain.Entities;

public class User : ITenantScoped
{
    public long Id { get; set; }
    public long OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string? JobTitle { get; set; }
    public UserStatus Status { get; set; } = UserStatus.ACTIVE;
    public long? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public long? OrganizationRoleId { get; set; }
    public OrganizationRole? OrganizationRole { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTimeOffset? LockoutUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
