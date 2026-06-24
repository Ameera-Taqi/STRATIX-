using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Stratix.Application.Interfaces;
using Stratix.Domain.Entities;

namespace Stratix.Infrastructure.Persistence;

public class StratixDbContext : DbContext, IApplicationDbContext
{
    private static readonly ValueConverter<DateTimeOffset, DateTime> DateTimeOffsetConverter = new(
        v => v.UtcDateTime,
        v => new DateTimeOffset(DateTime.SpecifyKind(v, DateTimeKind.Utc)));

    private static readonly ValueConverter<DateTimeOffset?, DateTime?> NullableDateTimeOffsetConverter = new(
        v => v.HasValue ? v.Value.UtcDateTime : null,
        v => v.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(v.Value, DateTimeKind.Utc)) : null);

    public StratixDbContext(DbContextOptions<StratixDbContext> options) : base(options) { }

    public DbSet<Department> DepartmentSet => Set<Department>();
    public DbSet<User> UserSet => Set<User>();
    public DbSet<Project> ProjectSet => Set<Project>();
    public DbSet<ProjectStage> ProjectStageSet => Set<ProjectStage>();
    public DbSet<TaskItem> TaskSet => Set<TaskItem>();
    public DbSet<ProjectRisk> ProjectRiskSet => Set<ProjectRisk>();
    public DbSet<AuditLog> AuditLogSet => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokenSet => Set<PasswordResetToken>();

    IQueryable<Department> IApplicationDbContext.Departments => DepartmentSet;
    IQueryable<User> IApplicationDbContext.Users => UserSet;
    IQueryable<Project> IApplicationDbContext.Projects => ProjectSet;
    IQueryable<ProjectStage> IApplicationDbContext.ProjectStages => ProjectStageSet;
    IQueryable<TaskItem> IApplicationDbContext.Tasks => TaskSet;
    IQueryable<ProjectRisk> IApplicationDbContext.ProjectRisks => ProjectRiskSet;
    IQueryable<AuditLog> IApplicationDbContext.AuditLogs => AuditLogSet;
    IQueryable<PasswordResetToken> IApplicationDbContext.PasswordResetTokens => PasswordResetTokenSet;

    void IApplicationDbContext.Add<T>(T entity) => Set<T>().Add(entity);
    void IApplicationDbContext.Remove<T>(T entity) => Set<T>().Remove(entity);

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
        Database.CanConnectAsync(cancellationToken);

    public string? GetDatabaseProductName()
    {
        try { return Database.ProviderName; }
        catch { return null; }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.JobTitle).HasColumnName("job_title").HasMaxLength(150);
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Department).WithMany(d => d.Users).HasForeignKey(x => x.DepartmentId);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            e.Property(x => x.Progress).HasColumnName("progress").HasPrecision(5, 2);
            e.Property(x => x.ProjectManagerId).HasColumnName("project_manager_id");
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.ProjectManager).WithMany().HasForeignKey(x => x.ProjectManagerId);
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        });

        modelBuilder.Entity<ProjectStage>(e =>
        {
            e.ToTable("project_stages");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProjectId).HasColumnName("project_id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.EndDate).HasColumnName("end_date");
            e.Property(x => x.Progress).HasColumnName("progress").HasPrecision(5, 2);
            e.Property(x => x.OrderNumber).HasColumnName("order_number");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Project).WithMany(p => p.Stages).HasForeignKey(x => x.ProjectId);
        });

        modelBuilder.Entity<TaskItem>(e =>
        {
            e.ToTable("tasks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ProjectId).HasColumnName("project_id");
            e.Property(x => x.StageId).HasColumnName("stage_id");
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.AssigneeId).HasColumnName("assignee_id");
            e.Property(x => x.StartDate).HasColumnName("start_date");
            e.Property(x => x.DueDate).HasColumnName("due_date");
            e.Property(x => x.CompletedAt).HasColumnName("completed_at");
            e.Property(x => x.Progress).HasColumnName("progress").HasPrecision(5, 2);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Project).WithMany(p => p.Tasks).HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId);
            e.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId);
        });

        modelBuilder.Entity<ProjectRisk>(e =>
        {
            e.ToTable("project_risks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Impact).HasColumnName("impact").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Probability).HasColumnName("probability").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RiskLevel).HasColumnName("risk_level").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.MitigationPlan).HasColumnName("mitigation_plan");
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.ProjectId).HasColumnName("project_id");
            e.Property(x => x.OwnerId).HasColumnName("owner_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Project).WithMany(p => p.Risks).HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.UserName).HasColumnName("user_name").HasMaxLength(200);
            e.Property(x => x.Action).HasColumnName("action").HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.EntityType).HasColumnName("entity_type").HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.EntityId).HasColumnName("entity_id");
            e.Property(x => x.EntityName).HasColumnName("entity_name").HasMaxLength(300);
            e.Property(x => x.OldValues).HasColumnName("old_values").HasColumnType("nvarchar(max)");
            e.Property(x => x.NewValues).HasColumnName("new_values").HasColumnType("nvarchar(max)");
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
            e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            e.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
            e.Property(x => x.ProjectId).HasColumnName("project_id");
            e.Property(x => x.ProjectName).HasColumnName("project_name").HasMaxLength(200);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<PasswordResetToken>(e =>
        {
            e.ToTable("password_reset_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.UsedAt).HasColumnName("used_at");
            e.Property(x => x.RequestIp).HasColumnName("request_ip").HasMaxLength(45);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset))
                    property.SetValueConverter(DateTimeOffsetConverter);
                else if (property.ClrType == typeof(DateTimeOffset?))
                    property.SetValueConverter(NullableDateTimeOffsetConverter);
            }
        }
    }
}
