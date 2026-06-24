using Stratix.Application.DTOs.Auth;
using Stratix.Application.DTOs.Departments;
using Stratix.Application.DTOs.Projects;
using Stratix.Application.DTOs.Risks;
using Stratix.Application.DTOs.Stages;
using Stratix.Application.DTOs.Tasks;
using Stratix.Application.DTOs.Users;
using Stratix.Application.DTOs.Audit;
using Stratix.Application.DTOs.Ai;
using Stratix.Application.DTOs;

using Stratix.Domain.Enums;

namespace Stratix.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(string username, string password, CancellationToken ct = default);
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
    Task<UserResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<UserResponse> UpdateAsync(long id, UpdateUserRequest request, CancellationToken ct = default);
}

public interface IProjectService
{
    Task<IReadOnlyList<ProjectResponse>> GetAllAsync(CancellationToken ct = default);
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
    Task<TaskResponse> GetByIdAsync(long id, CancellationToken ct = default);
    Task<TaskResponse> CreateAsync(CreateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateAsync(long id, UpdateTaskRequest request, CancellationToken ct = default);
    Task<TaskResponse> UpdateStatusAsync(long id, UpdateTaskStatusRequest request, CancellationToken ct = default);
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
}

public interface IHealthService
{
    Task<HealthResponse> GetHealthAsync(CancellationToken ct = default);
}

public interface IProjectHealthAnalysisService
{
    Task<ProjectHealthAnalysisResponse> AnalyzeAsync(ProjectHealthAnalysisRequest request, CancellationToken ct = default);
}
