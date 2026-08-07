using Stratix.Application.DTOs.Audit;
using Stratix.Application.DTOs.Auth;
using Stratix.Application.DTOs.EmployeeKpis;
using Stratix.Application.DTOs.Notifications;
using Stratix.Application.DTOs.ProjectFiles;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.DTOs.Risks;
using Stratix.Application.DTOs.Stages;
using Stratix.Application.DTOs.Tasks;
using Stratix.Application.DTOs.TaskComments;
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
        task.AssigneeId, task.Priority.ToString(), task.StartDate, task.DueDate,
        task.Status.ToString(), task.Description,
        task.EstimatedHours, task.ActualHours,
        task.BlockedReason, task.ReopenReason, task.ReviewReason,
        task.SubmittedForReviewAt, task.SubmittedForReviewById,
        task.SubmittedForReviewBy?.Name);

    public static TaskCommentResponse ToResponse(TaskComment c) => new(
        c.Id, c.TaskId, c.UserId, c.User?.Name ?? "", c.Comment, c.CreatedAt);

    public static EmployeeKpiResponse ToResponse(EmployeeKpi k) => new(
        k.Id, k.UserId, k.User?.Name ?? "", k.Period,
        k.TasksCompleted, k.TasksOnTime, k.Score, k.Notes, k.CreatedAt, k.UpdatedAt);

    public static NotificationResponse ToResponse(Notification n) => new(
        n.Id, n.UserId, n.Title, n.Message, n.Type.ToString(), n.IsRead, n.Link, n.CreatedAt,
        n.ActorName, n.ProjectName, n.EntityType, n.EntityId, n.EntityLabel);

    public static ProjectFileResponse ToResponse(ProjectFile f) => new(
        f.Id, f.ProjectId, f.Project?.Name ?? "", f.FileName, f.Description, f.Category,
        f.ContentType, f.SizeBytes, f.Url, f.UploadedById, f.UploadedBy?.Name ?? "", f.CreatedAt);

    public static RiskResponse ToResponse(ProjectRisk risk) => new(
        risk.Id, risk.Title, risk.Description, risk.Impact, risk.Probability,
        risk.RiskLevel, risk.MitigationPlan, risk.Status, risk.ProjectId,
        risk.Project?.Name ?? "", risk.OwnerId, risk.Owner?.Name ?? "",
        risk.CreatedAt, risk.UpdatedAt, risk.ClosureReason, risk.ResidualRisk);

    public static AuditLogResponse ToResponse(AuditLog log) => new(
        log.Id, log.UserId, log.UserName, log.Action, log.EntityType,
        log.EntityId, log.EntityName, log.OldValues, log.NewValues,
        log.Description, log.IpAddress, log.UserAgent, log.ProjectId,
        log.ProjectName, log.CreatedAt);
}
