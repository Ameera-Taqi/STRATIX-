using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Stratix.Application.Common;
using Stratix.Application.Interfaces;
using Stratix.Application.Services;
using Stratix.Application.Services.Reports;
using Stratix.Application.Validators;

namespace Stratix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.AddSingleton<Stratix.Application.Observability.IStratixMetrics, Stratix.Application.Observability.StratixMetrics>();
        services.AddScoped<OrganizationAccessService>();
        services.AddScoped<TenantRelationGuard>();
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
        services.AddSingleton<ITenantFileStorage, TenantFileStorage>();
        services.AddScoped<IReportFileStorage, ReportFileStorage>();
        services.AddScoped<ReportDataAssembler>();
        services.AddScoped<IReportDocumentBuilder, ReportDocumentBuilder>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IOrganizationRoleService, OrganizationRoleService>();
        services.AddScoped<IHealthService, HealthService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IProjectHealthAnalysisService, ProjectHealthAnalysisService>();
        services.AddScoped<IPlatformCmsService, PlatformCmsService>();
        services.AddScoped<IOrganizationBrandingService, OrganizationBrandingService>();
        services.AddScoped<IOrganizationOnboardingService, OrganizationOnboardingService>();
        services.AddScoped<IProgressRecalculationService, ProgressRecalculationService>();
        services.AddScoped<IProjectHealthSnapshotService, ProjectHealthSnapshotService>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddScoped<IKpiEvaluationService, KpiEvaluationService>();
        return services;
    }
}
