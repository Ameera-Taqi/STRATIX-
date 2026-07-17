using Microsoft.AspNetCore.Http;
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

    // Tenant isolation state, resolved once per request when the context is constructed.
    //  - Authenticated request  -> filter strictly by the user's "orgId" claim. A missing
    //    claim yields tenant id 0, which matches no real row (fail closed, never open).
    //  - No authenticated user  -> system context (startup seeding, background tasks) or the
    //    anonymous login lookup: the filter is bypassed so global operations can run.
    private readonly long _tenantId;
    private readonly bool _filterByTenant;

    public StratixDbContext(DbContextOptions<StratixDbContext> options, ITenantContext? tenant = null)
        : base(options)
    {
        // Normal authenticated user -> scope to their tenant (missing orgId => 0 => no rows).
        // Super-admins and system/anonymous callers are not scoped and see all tenants.
        if (tenant?.HasTenantScope == true)
        {
            _filterByTenant = true;
            _tenantId = tenant.OrganizationId ?? 0;
        }
    }

    public DbSet<Organization> OrganizationSet => Set<Organization>();
    public DbSet<Subscription> SubscriptionSet => Set<Subscription>();
    public DbSet<PlanTier> PlanSet => Set<PlanTier>();
    public DbSet<Department> DepartmentSet => Set<Department>();
    public DbSet<OrganizationRole> OrganizationRoleSet => Set<OrganizationRole>();
    public DbSet<User> UserSet => Set<User>();
    public DbSet<Project> ProjectSet => Set<Project>();
    public DbSet<ProjectStage> ProjectStageSet => Set<ProjectStage>();
    public DbSet<TaskItem> TaskSet => Set<TaskItem>();
    public DbSet<TaskComment> TaskCommentSet => Set<TaskComment>();
    public DbSet<ProjectRisk> ProjectRiskSet => Set<ProjectRisk>();
    public DbSet<Milestone> MilestoneSet => Set<Milestone>();
    public DbSet<ChangeRequest> ChangeRequestSet => Set<ChangeRequest>();
    public DbSet<EmployeeKpi> EmployeeKpiSet => Set<EmployeeKpi>();
    public DbSet<Notification> NotificationSet => Set<Notification>();
    public DbSet<ProjectFile> ProjectFileSet => Set<ProjectFile>();
    public DbSet<AuditLog> AuditLogSet => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokenSet => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokenSet => Set<RefreshToken>();
    public DbSet<PlatformModulePermission> PlatformModulePermissionSet => Set<PlatformModulePermission>();

    IQueryable<Organization> IApplicationDbContext.Organizations => OrganizationSet;
    IQueryable<Subscription> IApplicationDbContext.Subscriptions => SubscriptionSet;
    IQueryable<PlanTier> IApplicationDbContext.Plans => PlanSet;
    IQueryable<Department> IApplicationDbContext.Departments => DepartmentSet;
    IQueryable<OrganizationRole> IApplicationDbContext.OrganizationRoles => OrganizationRoleSet;
    IQueryable<User> IApplicationDbContext.Users => UserSet;
    IQueryable<Project> IApplicationDbContext.Projects => ProjectSet;
    IQueryable<ProjectStage> IApplicationDbContext.ProjectStages => ProjectStageSet;
    IQueryable<TaskItem> IApplicationDbContext.Tasks => TaskSet;
    IQueryable<TaskComment> IApplicationDbContext.TaskComments => TaskCommentSet;
    IQueryable<ProjectRisk> IApplicationDbContext.ProjectRisks => ProjectRiskSet;
    IQueryable<Milestone> IApplicationDbContext.Milestones => MilestoneSet;
    IQueryable<ChangeRequest> IApplicationDbContext.ChangeRequests => ChangeRequestSet;
    IQueryable<EmployeeKpi> IApplicationDbContext.EmployeeKpis => EmployeeKpiSet;
    IQueryable<Notification> IApplicationDbContext.Notifications => NotificationSet;
    IQueryable<ProjectFile> IApplicationDbContext.ProjectFiles => ProjectFileSet;
    IQueryable<AuditLog> IApplicationDbContext.AuditLogs => AuditLogSet;
    IQueryable<PasswordResetToken> IApplicationDbContext.PasswordResetTokens => PasswordResetTokenSet;
    IQueryable<RefreshToken> IApplicationDbContext.RefreshTokens => RefreshTokenSet;
    IQueryable<PlatformModulePermission> IApplicationDbContext.PlatformModulePermissions => PlatformModulePermissionSet;

    void IApplicationDbContext.Add<T>(T entity) => Set<T>().Add(entity);
    void IApplicationDbContext.Remove<T>(T entity) => Set<T>().Remove(entity);

    public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) =>
        Database.CanConnectAsync(cancellationToken);

    public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
        Database.BeginTransactionAsync(cancellationToken);

    public string? GetDatabaseProductName()
    {
        try { return Database.ProviderName; }
        catch { return null; }
    }

    public override int SaveChanges()
    {
        StampTenant();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(cancellationToken);
    }

    // Tenant guarantees enforced on every write:
    //  - INSERT: a new row inherits the caller's organization automatically, so a service can
    //    never accidentally insert into the wrong (or no) tenant.
    //  - UPDATE: OrganizationId is frozen — a payload can never move a row to another tenant,
    //    even if the client tampers with it.
    private void StampTenant()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added)
            {
                if (_filterByTenant && _tenantId != 0 && entry.Entity.OrganizationId == 0)
                    entry.Entity.OrganizationId = _tenantId;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(ITenantScoped.OrganizationId)).IsModified = false;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Organization>(e =>
        {
            e.ToTable("organizations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasColumnName("slug").HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Slug).IsUnique();
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.SubscriptionPlan).HasColumnName("subscription_plan").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.LogoFileName).HasColumnName("logo_file_name").HasMaxLength(255);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        });

        modelBuilder.Entity<Subscription>(e =>
        {
            e.ToTable("subscriptions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.HasIndex(x => x.OrganizationId).IsUnique();
            e.Property(x => x.PlanCode).HasColumnName("plan_code").HasMaxLength(50).IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.StartedAt).HasColumnName("started_at");
            e.Property(x => x.TrialEndsAt).HasColumnName("trial_ends_at");
            e.Property(x => x.EndsAt).HasColumnName("ends_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne<Organization>().WithOne().HasForeignKey<Subscription>(x => x.OrganizationId);
        });

        modelBuilder.Entity<PlanTier>(e =>
        {
            e.ToTable("subscription_plans");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
            e.Property(x => x.MaxUsers).HasColumnName("max_users");
            e.Property(x => x.MaxProjects).HasColumnName("max_projects");
            e.Property(x => x.AiEnabled).HasColumnName("ai_enabled");
            e.Property(x => x.StorageLimitMb).HasColumnName("storage_limit_mb");
            e.Property(x => x.Price).HasColumnName("price").HasPrecision(10, 2);
        });

        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            e.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<OrganizationRole>(e =>
        {
            e.ToTable("organization_roles");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            e.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique();
            e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
            e.Property(x => x.BaseRole).HasColumnName("base_role").HasMaxLength(50).IsRequired();
            e.Property(x => x.IsSystem).HasColumnName("is_system");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Password).HasColumnName("password").HasMaxLength(255).IsRequired();
            e.Property(x => x.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(50);
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
            e.Property(x => x.JobTitle).HasColumnName("job_title").HasMaxLength(150);
            e.Property(x => x.DepartmentId).HasColumnName("department_id");
            e.Property(x => x.OrganizationRoleId).HasColumnName("organization_role_id");
            e.Property(x => x.FailedLoginAttempts).HasColumnName("failed_login_attempts");
            e.Property(x => x.LockoutUntil).HasColumnName("lockout_until");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Department).WithMany(d => d.Users).HasForeignKey(x => x.DepartmentId);
            e.HasOne(x => x.OrganizationRole).WithMany().HasForeignKey(x => x.OrganizationRoleId);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("projects");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
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
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
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
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
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

        modelBuilder.Entity<TaskComment>(e =>
        {
            e.ToTable("task_comments");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.TaskId).HasColumnName("task_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Comment).HasColumnName("comment").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
            e.HasIndex(x => x.TaskId);
        });

        modelBuilder.Entity<ProjectRisk>(e =>
        {
            e.ToTable("project_risks");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
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

        modelBuilder.Entity<Milestone>(e =>
        {
            e.ToTable("project_milestones");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.ProjectId).HasColumnName("project_id");
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            e.Property(x => x.DueDate).HasColumnName("due_date");
            e.Property(x => x.CompletedDate).HasColumnName("completed_date");
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        });

        modelBuilder.Entity<ChangeRequest>(e =>
        {
            e.ToTable("change_requests");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.ProjectId).HasColumnName("project_id");
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasColumnName("description");
            e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.Priority).HasColumnName("priority").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.RequestedById).HasColumnName("requested_by_id");
            e.Property(x => x.ReviewedById).HasColumnName("reviewed_by_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedById);
            e.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById);
        });

        modelBuilder.Entity<EmployeeKpi>(e =>
        {
            e.ToTable("employee_kpis");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Period).HasColumnName("period").HasMaxLength(20).IsRequired();
            e.Property(x => x.TasksCompleted).HasColumnName("tasks_completed");
            e.Property(x => x.TasksOnTime).HasColumnName("tasks_on_time");
            e.Property(x => x.Score).HasColumnName("score").HasPrecision(5, 2);
            e.Property(x => x.Notes).HasColumnName("notes");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.ToTable("notifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
            e.Property(x => x.Message).HasColumnName("message");
            e.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
            e.Property(x => x.IsRead).HasColumnName("is_read");
            e.Property(x => x.Link).HasColumnName("link").HasMaxLength(500);
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<ProjectFile>(e =>
        {
            e.ToTable("project_files");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
            e.Property(x => x.ProjectId).HasColumnName("project_id");
            e.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(300).IsRequired();
            e.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(150);
            e.Property(x => x.SizeBytes).HasColumnName("size_bytes");
            e.Property(x => x.Url).HasColumnName("url").HasMaxLength(1000).IsRequired();
            e.Property(x => x.UploadedById).HasColumnName("uploaded_by_id");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
            e.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById);
        });

        modelBuilder.Entity<AuditLog>(e =>
        {
            e.ToTable("audit_logs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.OrganizationId).HasColumnName("organization_id");
            e.HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
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

        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
            e.HasIndex(x => x.TokenHash);
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
            e.Property(x => x.CreatedAt).HasColumnName("created_at");
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            e.Ignore(x => x.IsActive);
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

        modelBuilder.Entity<PlatformModulePermission>(e =>
        {
            e.ToTable("platform_module_permissions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("id");
            e.Property(x => x.ModuleCode).HasColumnName("module_code").HasMaxLength(50).IsRequired();
            e.HasIndex(x => x.ModuleCode).IsUnique();
            e.Property(x => x.VisibleToCompanyAdmin).HasColumnName("visible_to_company_admin");
            e.Property(x => x.WritableByCompanyAdmin).HasColumnName("writable_by_company_admin");
            e.Property(x => x.SortOrder).HasColumnName("sort_order");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
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
