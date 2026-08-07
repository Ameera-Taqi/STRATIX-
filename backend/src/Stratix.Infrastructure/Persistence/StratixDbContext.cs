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
    private readonly long _stampTenantId;
    private readonly bool _filterByTenant;

    public StratixDbContext(DbContextOptions<StratixDbContext> options, ITenantContext? tenant = null)
        : base(options)
    {
        // Normal authenticated user -> scope to their tenant (missing orgId => 0 => no rows).
        // Super-admins and system/anonymous callers are not scoped and see all tenants.
        // Stamp fallback still uses the JWT orgId when present (SUPER_ADMIN home tenant).
        _stampTenantId = tenant?.OrganizationId ?? 0;
        if (tenant?.HasTenantScope == true)
        {
            _filterByTenant = true;
            _tenantId = _stampTenantId;
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
    public DbSet<EmployeeKpi> EmployeeKpiSet => Set<EmployeeKpi>();
    public DbSet<Notification> NotificationSet => Set<Notification>();
    public DbSet<ProjectFile> ProjectFileSet => Set<ProjectFile>();
    public DbSet<Report> ReportSet => Set<Report>();
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
    IQueryable<EmployeeKpi> IApplicationDbContext.EmployeeKpis => EmployeeKpiSet;
    IQueryable<Notification> IApplicationDbContext.Notifications => NotificationSet;
    IQueryable<ProjectFile> IApplicationDbContext.ProjectFiles => ProjectFileSet;
    IQueryable<Report> IApplicationDbContext.Reports => ReportSet;
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
        ApplyPersistencePolicies();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyPersistencePolicies();
        return base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Persistence policy order:
    /// 1) Soft delete — convert hard deletes on <see cref="ISoftDeletable"/> to flag updates.
    /// 2) Tenant stamp — force OrganizationId on insert; freeze it on update.
    /// 3) Audit timestamps — stamp CreatedAt / UpdatedAt.
    /// </summary>
    private void ApplyPersistencePolicies()
    {
        ApplySoftDelete();
        StampTenant();
        ApplyAuditTimestamps();
    }

    private void ApplySoftDelete()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAt = now;
        }
    }

    // Tenant guarantees enforced on every write:
    //  - INSERT (scoped): OrganizationId is ALWAYS forced to the caller's tenant
    //    (client/service values cannot override or cross tenants).
    //  - INSERT (unscoped / SUPER_ADMIN / system): keep an explicit OrganizationId if set;
    //    otherwise fall back to the JWT org claim; never allow 0.
    //  - UPDATE: OrganizationId is frozen — a payload can never move a row to another tenant.
    private void StampTenant()
    {
        foreach (var entry in ChangeTracker.Entries<ITenantScoped>())
        {
            if (entry.State == EntityState.Added)
            {
                if (_filterByTenant)
                {
                    if (_tenantId == 0)
                        throw new InvalidOperationException(
                            "Cannot insert tenant-scoped data without a valid OrganizationId claim.");
                    entry.Entity.OrganizationId = _tenantId;
                }
                else if (entry.Entity.OrganizationId == 0)
                {
                    if (_stampTenantId != 0)
                        entry.Entity.OrganizationId = _stampTenantId;
                    else
                        throw new InvalidOperationException(
                            $"OrganizationId must be set when inserting {entry.Entity.GetType().Name} outside a tenant scope.");
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(ITenantScoped.OrganizationId)).IsModified = false;
            }
        }
    }

    private void ApplyAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is IHasCreatedAt created && created.CreatedAt == default)
                    created.CreatedAt = now;
                if (entry.Entity is IHasUpdatedAt updated && updated.UpdatedAt == default)
                    updated.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified && entry.Entity is IHasUpdatedAt touch)
            {
                touch.UpdatedAt = now;
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StratixDbContext).Assembly);
        ApplyQueryFilters(modelBuilder);

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

    private void ApplyQueryFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Subscription>().HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
        modelBuilder.Entity<Department>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<OrganizationRole>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<Project>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<ProjectStage>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<TaskItem>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<TaskComment>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<ProjectRisk>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<EmployeeKpi>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<Notification>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<ProjectFile>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<Report>().HasQueryFilter(x => (!_filterByTenant || x.OrganizationId == _tenantId) && !x.IsDeleted);
        modelBuilder.Entity<AuditLog>().HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
        modelBuilder.Entity<RefreshToken>().HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
        modelBuilder.Entity<PasswordResetToken>().HasQueryFilter(x => !_filterByTenant || x.OrganizationId == _tenantId);
    }
}
