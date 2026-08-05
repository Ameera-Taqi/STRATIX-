using Microsoft.Extensions.DependencyInjection;
using Stratix.Application.Interfaces;
using Stratix.Application.Services;

namespace Stratix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IOrganizationService, OrganizationService>();
        services.AddScoped<IPlanLimitService, PlanLimitService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IStageService, StageService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<ITaskCommentService, TaskCommentService>();
        services.AddScoped<IRiskService, RiskService>();
        services.AddScoped<IEmployeeKpiService, EmployeeKpiService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IProjectFileService, ProjectFileService>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IOrganizationRoleService, OrganizationRoleService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IProjectHealthAnalysisService, ProjectHealthAnalysisService>();
        services.AddScoped<IPlatformCmsService, PlatformCmsService>();
        services.AddScoped<IOrganizationBrandingService, OrganizationBrandingService>();
        return services;
    }
}
