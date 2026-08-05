using Stratix.Domain.Entities;

namespace Stratix.Application.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<Organization> Organizations { get; }
    IQueryable<Subscription> Subscriptions { get; }
    IQueryable<PlanTier> Plans { get; }
    IQueryable<Department> Departments { get; }
    IQueryable<OrganizationRole> OrganizationRoles { get; }
    IQueryable<User> Users { get; }
    IQueryable<Project> Projects { get; }
    IQueryable<ProjectStage> ProjectStages { get; }
    IQueryable<TaskItem> Tasks { get; }
    IQueryable<TaskComment> TaskComments { get; }
    IQueryable<ProjectRisk> ProjectRisks { get; }
    IQueryable<EmployeeKpi> EmployeeKpis { get; }
    IQueryable<Notification> Notifications { get; }
    IQueryable<ProjectFile> ProjectFiles { get; }
    IQueryable<AuditLog> AuditLogs { get; }
    IQueryable<PasswordResetToken> PasswordResetTokens { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }
    IQueryable<PlatformModulePermission> PlatformModulePermissions { get; }

    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    string? GetDatabaseProductName();
}
