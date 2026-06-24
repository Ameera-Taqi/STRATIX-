using Microsoft.Extensions.DependencyInjection;
using Stratix.Application.Interfaces;
using Stratix.Application.Services;

namespace Stratix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IStageService, StageService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IRiskService, RiskService>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IProjectHealthAnalysisService, ProjectHealthAnalysisService>();
        return services;
    }
}
