using Stratix.Application.DTOs.Audit;
using Stratix.Application.DTOs.Auth;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.DTOs.Risks;
using Stratix.Application.DTOs.Stages;
using Stratix.Application.DTOs.Tasks;
using Stratix.Application.DTOs.Users;
using Stratix.Application.Mapping;
using Stratix.Domain.Entities;

namespace Stratix.Application.Mapping;

public static class EntityMappers
{
    public static AuthUserProfile ToProfile(User user) => new(
        user.Id, user.Name, user.Email,
        EnumDisplayMapper.UserRoleDisplay(user.Role),
        user.Role.ToString(),
        user.Department?.Name ?? "",
        user.Id);

    public static UserResponse ToResponse(User user) => new(
        user.Id, user.Name, user.Email, user.Role, user.JobTitle, user.Status,
        user.DepartmentId, user.Department?.Name, user.CreatedAt, user.UpdatedAt);

    public static ProjectResponse ToResponse(Project project)
    {
        var manager = project.ProjectManager?.Name ?? "";
        var managerId = project.ProjectManagerId;
        var department = project.Department?.Name ?? "";
        var progress = (int)project.Progress;
        return new ProjectResponse(
            project.Id, project.Name, department, manager, managerId,
            project.StartDate, project.EndDate, progress,
            EnumDisplayMapper.ProjectStatusDisplay(project.Status),
            EnumDisplayMapper.ProjectPriorityCode(project.Priority),
            manager, project.EndDate);
    }

    public static StageResponse ToResponse(ProjectStage stage) => new(
        stage.Id, stage.ProjectId, stage.Name, stage.Description,
        stage.StartDate, stage.EndDate, stage.Progress,
        EnumDisplayMapper.StageStatusDisplay(stage.Status), stage.OrderNumber);

    public static TaskResponse ToResponse(TaskItem task) => new(
        task.Id, task.ProjectId, task.Project?.Name ?? "", task.StageId,
        task.Stage?.Name, task.Title, task.Assignee?.Name ?? "",
        task.AssigneeId, task.Priority.ToString(), task.DueDate,
        task.Status.ToString(), task.Description);

    public static RiskResponse ToResponse(ProjectRisk risk) => new(
        risk.Id, risk.Title, risk.Description, risk.Impact, risk.Probability,
        risk.RiskLevel, risk.MitigationPlan, risk.Status, risk.ProjectId,
        risk.Project?.Name ?? "", risk.OwnerId, risk.Owner?.Name ?? "",
        risk.CreatedAt, risk.UpdatedAt);

    public static AuditLogResponse ToResponse(AuditLog log) => new(
        log.Id, log.UserId, log.UserName, log.Action, log.EntityType,
        log.EntityId, log.EntityName, log.OldValues, log.NewValues,
        log.Description, log.IpAddress, log.UserAgent, log.ProjectId,
        log.ProjectName, log.CreatedAt);
}
