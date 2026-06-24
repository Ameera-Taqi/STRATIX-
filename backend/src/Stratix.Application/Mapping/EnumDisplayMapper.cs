using Stratix.Domain.Enums;

namespace Stratix.Application.Mapping;

public static class EnumDisplayMapper
{
    public static string ProjectStatusDisplay(ProjectStatus status) => status switch
    {
        ProjectStatus.PLANNED => "Planned",
        ProjectStatus.ACTIVE => "Active",
        ProjectStatus.ON_HOLD => "On Hold",
        ProjectStatus.COMPLETED => "Done",
        ProjectStatus.CANCELLED => "Cancelled",
        _ => ""
    };

    public static string StageStatusDisplay(StageStatus status) => status switch
    {
        StageStatus.PLANNED => "Planned",
        StageStatus.ACTIVE => "Active",
        StageStatus.DONE => "Done",
        StageStatus.ON_HOLD => "On Hold",
        _ => ""
    };

    public static string ProjectPriorityCode(ProjectPriority priority) =>
        priority == ProjectPriority.CRITICAL ? "URGENT" : priority.ToString();

    public static string UserRoleDisplay(UserRole role) => role switch
    {
        UserRole.ADMIN => "Admin",
        UserRole.PROJECT_MANAGER => "Project Manager",
        UserRole.TEAM_LEADER => "Team Leader",
        UserRole.EMPLOYEE => "Employee",
        UserRole.EXECUTIVE_VIEWER => "Executive Viewer",
        _ => ""
    };
}
