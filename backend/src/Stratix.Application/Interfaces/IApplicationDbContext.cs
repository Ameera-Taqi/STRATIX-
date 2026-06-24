using Stratix.Domain.Entities;

namespace Stratix.Application.Interfaces;

public interface IApplicationDbContext
{
    IQueryable<Department> Departments { get; }
    IQueryable<User> Users { get; }
    IQueryable<Project> Projects { get; }
    IQueryable<ProjectStage> ProjectStages { get; }
    IQueryable<TaskItem> Tasks { get; }
    IQueryable<ProjectRisk> ProjectRisks { get; }
    IQueryable<AuditLog> AuditLogs { get; }
    IQueryable<PasswordResetToken> PasswordResetTokens { get; }

    void Add<T>(T entity) where T : class;
    void Remove<T>(T entity) where T : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
    string? GetDatabaseProductName();
}
