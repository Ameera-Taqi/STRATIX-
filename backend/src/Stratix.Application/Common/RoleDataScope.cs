using Microsoft.EntityFrameworkCore;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;

namespace Stratix.Application.Common;

/// <summary>Row-level visibility rules by role within the current tenant.</summary>
public static class RoleDataScope
{
    public static IQueryable<Project> Apply(IQueryable<Project> query, long? userId, UserRole? role)
    {
        if (userId is null or <= 0 || role is null) return query.Where(_ => false);

        return role switch
        {
            UserRole.SUPER_ADMIN or UserRole.ORG_ADMIN or UserRole.ADMIN or UserRole.EXECUTIVE_VIEWER
                => query,
            UserRole.PROJECT_MANAGER
                => query.Where(p => p.ProjectManagerId == userId),
            UserRole.TEAM_LEADER or UserRole.EMPLOYEE
                => query.Where(p =>
                    p.ProjectManagerId == userId ||
                    p.Tasks.Any(t => t.AssigneeId == userId)),
            _ => query.Where(_ => false),
        };
    }

    public static IQueryable<TaskItem> Apply(IQueryable<TaskItem> query, long? userId, UserRole? role)
    {
        if (userId is null or <= 0 || role is null) return query.Where(_ => false);

        return role switch
        {
            UserRole.SUPER_ADMIN or UserRole.ORG_ADMIN or UserRole.ADMIN or UserRole.EXECUTIVE_VIEWER
                => query,
            UserRole.PROJECT_MANAGER
                => query.Where(t => t.Project.ProjectManagerId == userId),
            UserRole.TEAM_LEADER
                => query.Where(t =>
                    t.Project.ProjectManagerId == userId ||
                    t.AssigneeId == userId ||
                    t.Project.Tasks.Any(x => x.AssigneeId == userId)),
            UserRole.EMPLOYEE
                => query.Where(t => t.AssigneeId == userId),
            _ => query.Where(_ => false),
        };
    }

    public static IQueryable<ProjectRisk> Apply(IQueryable<ProjectRisk> query, long? userId, UserRole? role)
    {
        if (userId is null or <= 0 || role is null) return query.Where(_ => false);

        return role switch
        {
            UserRole.SUPER_ADMIN or UserRole.ORG_ADMIN or UserRole.ADMIN or UserRole.EXECUTIVE_VIEWER
                => query,
            UserRole.PROJECT_MANAGER
                => query.Where(r => r.Project.ProjectManagerId == userId),
            UserRole.TEAM_LEADER or UserRole.EMPLOYEE
                => query.Where(r =>
                    r.OwnerId == userId ||
                    r.Project.ProjectManagerId == userId ||
                    r.Project.Tasks.Any(t => t.AssigneeId == userId)),
            _ => query.Where(_ => false),
        };
    }
}
