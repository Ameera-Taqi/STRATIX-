using Stratix.Application.DTOs.Auth;
using Stratix.Application.DTOs.Branding;
using Stratix.Application.DTOs.Cms;
using Stratix.Application.DTOs.Departments;
using Stratix.Application.DTOs.Roles;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.DTOs.Risks;
using Stratix.Application.DTOs.Stages;
using Stratix.Application.DTOs.Tasks;
using Stratix.Application.DTOs.TaskComments;
using Stratix.Application.DTOs.Users;
using Stratix.Application.DTOs.Audit;
using Stratix.Application.DTOs.Ai;
using Stratix.Application.DTOs;

using Stratix.Domain.Enums;

namespace Stratix.Application.Interfaces;

public interface IMilestoneService
{
    Task<IReadOnlyList<Stratix.Application.DTOs.Milestones.MilestoneResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Stratix.Application.DTOs.Milestones.MilestoneResponse>> GetByProjectAsync(long projectId, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.Milestones.MilestoneResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.Milestones.MilestoneResponse> CreateAsync(Stratix.Application.DTOs.Milestones.CreateMilestoneRequest request, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.Milestones.MilestoneResponse> UpdateAsync(long id, Stratix.Application.DTOs.Milestones.UpdateMilestoneRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IChangeRequestService
{
    Task<IReadOnlyList<Stratix.Application.DTOs.ChangeRequests.ChangeRequestResponse>> GetAllAsync(long? projectId, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.ChangeRequests.ChangeRequestResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.ChangeRequests.ChangeRequestResponse> CreateAsync(Stratix.Application.DTOs.ChangeRequests.CreateChangeRequestRequest request, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.ChangeRequests.ChangeRequestResponse> UpdateAsync(long id, Stratix.Application.DTOs.ChangeRequests.UpdateChangeRequestRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IEmployeeKpiService
{
    Task<IReadOnlyList<Stratix.Application.DTOs.EmployeeKpis.EmployeeKpiResponse>> GetAllAsync(long? userId, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.EmployeeKpis.EmployeeKpiResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.EmployeeKpis.EmployeeKpiResponse> CreateAsync(Stratix.Application.DTOs.EmployeeKpis.CreateEmployeeKpiRequest request, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.EmployeeKpis.EmployeeKpiResponse> UpdateAsync(long id, Stratix.Application.DTOs.EmployeeKpis.UpdateEmployeeKpiRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface INotificationService
{
    Task<IReadOnlyList<Stratix.Application.DTOs.Notifications.NotificationResponse>> GetMineAsync(CancellationToken ct = default);
    Task<Stratix.Application.DTOs.Notifications.NotificationResponse> CreateAsync(Stratix.Application.DTOs.Notifications.CreateNotificationRequest request, CancellationToken ct = default);
    Task MarkReadAsync(long id, CancellationToken ct = default);
    Task MarkAllReadAsync(CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IProjectFileService
{
    Task<IReadOnlyList<Stratix.Application.DTOs.ProjectFiles.ProjectFileResponse>> GetByProjectAsync(long projectId, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.ProjectFiles.ProjectFileResponse> CreateAsync(Stratix.Application.DTOs.ProjectFiles.CreateProjectFileRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IOrganizationService
{
    Task<Stratix.Application.DTOs.Organizations.OrganizationResponse?> GetCurrentAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Stratix.Application.DTOs.Organizations.OrganizationResponse>> GetAllAsync(CancellationToken ct = default);
    Task<Stratix.Application.DTOs.Organizations.OrganizationResponse> CreateAsync(Stratix.Application.DTOs.Organizations.CreateOrganizationRequest request, CancellationToken ct = default);
    Task<Stratix.Application.DTOs.Organizations.OrganizationResponse?> UpdateAsync(long id, Stratix.Application.DTOs.Organizations.UpdateOrganizationRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<Stratix.Application.DTOs.Organizations.PlanResponse>> GetPlansAsync(CancellationToken ct = default);
    Task<Stratix.Application.DTOs.Organizations.SubscriptionResponse?> GetCurrentSubscriptionAsync(CancellationToken ct = default);
}

public interface IAuthService
{
    Task<LoginResponse> RegisterOrganizationAsync(RegisterOrganizationRequest request, CancellationToken ct = default);
    Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct = default);
    Task<LoginResponse> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
    Task<AuthUserProfile> GetCurrentUserAsync(CancellationToken ct = default);
    Task<AuthUserProfile> UpdateMyProfileAsync(UpdateMyProfileRequest request, CancellationToken ct = default);
}

public interface IPasswordResetService
{
    Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, string? ip, CancellationToken ct = default);
    Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}

public interface IUserService
{
    Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken ct = default);
    Task<Stratix.Application.Common.PagedResult<UserResponse>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserResponse> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default);
}

public interface IProjectService
{
    Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken ct = default);
    Task<Stratix.Application.Common.PagedResult<ProjectResponse>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
    Task<ProjectResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<ProjectResponse> CreateAsync(CreateProjectRequest request, CancellationToken ct = default);
    Task<ProjectResponse> UpdateAsync(long id, UpdateProjectRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IStageService
{
    Task<IReadOnlyList<StageResponse>> GetByProjectAsync(long projectId, CancellationToken ct = default);
    Task<StageResponse> CreateAsync(long projectId, CreateStageRequest request, CancellationToken ct = default);
    Task<StageResponse> UpdateAsync(long id, UpdateStageRequest request, CancellationToken ct = default);
    Task<StageResponse> CompleteAsync(long id, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> GetAllAsync(long? projectId, CancellationToken ct = default);
    Task<Stratix.Application.Common.PagedResult<TaskResponse>> GetPagedAsync(long? projectId, int page, int pageSize, CancellationToken ct = default);
    Task<TaskResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateAsync(long id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateStatusAsync(long id, UpdateTaskStatusRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface ITaskCommentService
{
    Task<IReadOnlyList<TaskCommentResponse>> GetByTaskAsync(long taskId, CancellationToken ct = default);
    Task<TaskCommentResponse> CreateAsync(CreateTaskCommentRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IRiskService
{
    Task<IReadOnlyList<RiskResponse>> GetAllAsync(CancellationToken ct = default);
    Task<RiskResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<RiskResponse> CreateAsync(CreateRiskRequest request, CancellationToken ct = default);
    Task<RiskResponse> UpdateAsync(long id, UpdateRiskRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
    Task<RiskDashboardStatsResponse> GetDashboardStatsAsync(CancellationToken ct = default);
    Task<RiskHeatMapResponse> GetHeatMapAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RiskResponse>> GetByProjectAsync(long projectId, CancellationToken ct = default);
    Task<IReadOnlyList<RiskResponse>> GetByOwnerAsync(long ownerId, CancellationToken ct = default);
    Task<IReadOnlyList<RiskResponse>> GetOpenAsync(CancellationToken ct = default);
    Task<IReadOnlyList<RiskResponse>> GetCriticalAsync(CancellationToken ct = default);
}

public interface IAuditTrailService
{
    Task<AuditLogPageResponse> SearchAsync(AuditLogQuery query, CancellationToken ct = default);
    Task RecordCreateAsync(AuditEntityType entityType, long entityId, string entityName, string? snapshot, string description, long? projectId, string? projectName, CancellationToken ct = default);
    Task RecordDeleteAsync(AuditEntityType entityType, long entityId, string entityName, string? snapshot, string description, long? projectId, string? projectName, CancellationToken ct = default);
}

public record AuditLogQuery(
    string? EntityType, long? EntityId, long? UserId, string? Action, long? ProjectId,
    DateTimeOffset? StartDate, DateTimeOffset? EndDate, string? Search, int Page, int Size);

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentResponse>> GetAllAsync(CancellationToken ct = default);
    Task<DepartmentResponse> CreateAsync(CreateDepartmentRequest request, CancellationToken ct = default);
    Task<DepartmentResponse> UpdateAsync(long id, UpdateDepartmentRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IOrganizationRoleService
{
    Task<IReadOnlyList<OrganizationRoleResponse>> GetAllAsync(CancellationToken ct = default);
    Task<OrganizationRoleResponse> CreateAsync(CreateOrganizationRoleRequest request, CancellationToken ct = default);
    Task<OrganizationRoleResponse> UpdateAsync(long id, UpdateOrganizationRoleRequest request, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public interface IHealthService
{
    Task<HealthResponse> GetHealthAsync(CancellationToken ct = default);
}

public interface IProjectHealthAnalysisService
{
    Task<ProjectHealthAnalysisResponse> AnalyzeAsync(ProjectHealthAnalysisRequest request, CancellationToken ct = default);
}

public interface IPlatformCmsService
{
    Task<IReadOnlyList<CompanyAdminModulePermissionResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<CompanyAdminModulePermissionResponse>> UpdateAsync(UpdateCompanyAdminPermissionsRequest request, CancellationToken ct = default);
    Task EnsureDefaultsAsync(CancellationToken ct = default);
}

public interface IOrganizationBrandingService
{
    Task<OrganizationBrandingResponse> GetAsync(CancellationToken ct = default);
    Task<(Stream Stream, string ContentType, string FileName)?> OpenLogoAsync(CancellationToken ct = default);
    Task<OrganizationLogoUploadResult> UploadAsync(
        Stream content,
        string contentType,
        string originalFileName,
        long length,
        CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);
}
