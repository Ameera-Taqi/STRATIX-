using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Stratix.Domain.Entities;

namespace Stratix.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> e)
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
    }
}

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> e)
    {
        e.ToTable("subscriptions");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.HasIndex(x => x.OrganizationId).IsUnique();
        e.Property(x => x.PlanCode).HasColumnName("plan_code").HasMaxLength(50).IsRequired();
        e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(30);
        e.Property(x => x.StartedAt).HasColumnName("started_at");
        e.Property(x => x.TrialEndsAt).HasColumnName("trial_ends_at");
        e.Property(x => x.EndsAt).HasColumnName("ends_at");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.HasOne<Organization>().WithOne().HasForeignKey<Subscription>(x => x.OrganizationId);
    }
}

public class PlanTierConfiguration : IEntityTypeConfiguration<PlanTier>
{
    public void Configure(EntityTypeBuilder<PlanTier> e)
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
    }
}

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> e)
    {
        e.ToTable("departments");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        e.HasIndex(x => new { x.OrganizationId, x.Name }).IsUnique().HasFilter("[is_deleted] = 0");
        e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
    }
}

public class OrganizationRoleConfiguration : IEntityTypeConfiguration<OrganizationRole>
{
    public void Configure(EntityTypeBuilder<OrganizationRole> e)
    {
        e.ToTable("organization_roles");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        e.HasIndex(x => new { x.OrganizationId, x.Code }).IsUnique().HasFilter("[is_deleted] = 0");
        e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        e.Property(x => x.BaseRole).HasColumnName("base_role").HasMaxLength(50).IsRequired();
        e.Property(x => x.IsSystem).HasColumnName("is_system");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> e)
    {
        e.ToTable("users");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255).IsRequired();
        // Settled policy: one active account per email globally (login resolves by email alone).
        e.HasIndex(x => x.Email).IsUnique().HasFilter("[is_deleted] = 0");
        e.HasIndex(x => new { x.OrganizationId, x.Status });
        e.HasIndex(x => new { x.OrganizationId, x.DepartmentId });
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
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Department).WithMany(d => d.Users).HasForeignKey(x => x.DepartmentId);
        e.HasOne(x => x.OrganizationRole).WithMany().HasForeignKey(x => x.OrganizationRoleId);
    }
}

public class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> e)
    {
        e.ToTable("projects");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
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
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.ProjectManager).WithMany().HasForeignKey(x => x.ProjectManagerId);
        e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId);
        e.HasIndex(x => new { x.OrganizationId, x.Status });
        e.HasIndex(x => new { x.OrganizationId, x.DepartmentId });
        e.HasIndex(x => new { x.OrganizationId, x.ProjectManagerId });
    }
}

public class ProjectStageConfiguration : IEntityTypeConfiguration<ProjectStage>
{
    public void Configure(EntityTypeBuilder<ProjectStage> e)
    {
        e.ToTable("project_stages");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
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
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Project).WithMany(p => p.Stages).HasForeignKey(x => x.ProjectId);
        e.HasIndex(x => new { x.OrganizationId, x.ProjectId });
    }
}

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> e)
    {
        e.ToTable("tasks");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
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
        e.Property(x => x.EstimatedHours).HasColumnName("estimated_hours").HasPrecision(10, 2);
        e.Property(x => x.ActualHours).HasColumnName("actual_hours").HasPrecision(10, 2);
        e.Property(x => x.BlockedReason).HasColumnName("blocked_reason").HasMaxLength(1000);
        e.Property(x => x.ReopenReason).HasColumnName("reopen_reason").HasMaxLength(1000);
        e.Property(x => x.ReviewReason).HasColumnName("review_reason").HasMaxLength(1000);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Project).WithMany(p => p.Tasks).HasForeignKey(x => x.ProjectId);
        e.HasOne(x => x.Stage).WithMany().HasForeignKey(x => x.StageId);
        e.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeId);
        e.HasIndex(x => new { x.OrganizationId, x.ProjectId });
        e.HasIndex(x => new { x.OrganizationId, x.Status });
        e.HasIndex(x => new { x.OrganizationId, x.AssigneeId });
    }
}

public class TaskCommentConfiguration : IEntityTypeConfiguration<TaskComment>
{
    public void Configure(EntityTypeBuilder<TaskComment> e)
    {
        e.ToTable("task_comments");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.TaskId).HasColumnName("task_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.Comment).HasColumnName("comment").IsRequired();
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x => x.TaskId);
        e.HasIndex(x => new { x.OrganizationId, x.TaskId });
    }
}

public class ProjectRiskConfiguration : IEntityTypeConfiguration<ProjectRisk>
{
    public void Configure(EntityTypeBuilder<ProjectRisk> e)
    {
        e.ToTable("project_risks");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
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
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Project).WithMany(p => p.Risks).HasForeignKey(x => x.ProjectId);
        e.HasOne(x => x.Owner).WithMany().HasForeignKey(x => x.OwnerId);
        e.HasIndex(x => new { x.OrganizationId, x.ProjectId });
        e.HasIndex(x => new { x.OrganizationId, x.Status });
        e.HasIndex(x => new { x.OrganizationId, x.OwnerId });
    }
}

public class EmployeeKpiConfiguration : IEntityTypeConfiguration<EmployeeKpi>
{
    public void Configure(EntityTypeBuilder<EmployeeKpi> e)
    {
        e.ToTable("employee_kpis");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.Period).HasColumnName("period").HasMaxLength(20).IsRequired();
        e.Property(x => x.TasksCompleted).HasColumnName("tasks_completed");
        e.Property(x => x.TasksOnTime).HasColumnName("tasks_on_time");
        e.Property(x => x.Score).HasColumnName("score").HasPrecision(5, 2);
        e.Property(x => x.Notes).HasColumnName("notes");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        // One KPI evaluation per employee per period within a tenant (active rows only).
        e.HasIndex(x => new { x.OrganizationId, x.UserId, x.Period })
            .IsUnique()
            .HasFilter("[is_deleted] = 0");
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> e)
    {
        e.ToTable("notifications");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
        e.Property(x => x.Message).HasColumnName("message");
        e.Property(x => x.Type).HasColumnName("type").HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.IsRead).HasColumnName("is_read");
        e.Property(x => x.Link).HasColumnName("link").HasMaxLength(500);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        e.HasIndex(x => new { x.OrganizationId, x.UserId, x.IsRead });
    }
}

public class ProjectFileConfiguration : IEntityTypeConfiguration<ProjectFile>
{
    public void Configure(EntityTypeBuilder<ProjectFile> e)
    {
        e.ToTable("project_files");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.ProjectId).HasColumnName("project_id");
        e.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(300).IsRequired();
        e.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000);
        e.Property(x => x.Category).HasColumnName("category").HasMaxLength(80);
        e.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(150);
        e.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        e.Property(x => x.Url).HasColumnName("url").HasMaxLength(1000).IsRequired();
        e.Property(x => x.UploadedById).HasColumnName("uploaded_by_id");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        e.HasOne(x => x.UploadedBy).WithMany().HasForeignKey(x => x.UploadedById);
        e.HasIndex(x => new { x.OrganizationId, x.ProjectId });
    }
}

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> e)
    {
        e.ToTable("reports");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.Title).HasColumnName("title").HasMaxLength(300).IsRequired();
        e.Property(x => x.ReportType).HasColumnName("report_type").HasConversion<string>().HasMaxLength(50);
        e.Property(x => x.Format).HasColumnName("format").HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.ProjectId).HasColumnName("project_id");
        e.Property(x => x.DepartmentId).HasColumnName("department_id");
        e.Property(x => x.EmployeeId).HasColumnName("employee_id");
        e.Property(x => x.DateFrom).HasColumnName("date_from");
        e.Property(x => x.DateTo).HasColumnName("date_to");
        e.Property(x => x.FileName).HasColumnName("file_name").HasMaxLength(300).IsRequired();
        e.Property(x => x.StorageKey).HasColumnName("storage_key").HasMaxLength(500).IsRequired();
        e.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(150);
        e.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        e.Property(x => x.GeneratedById).HasColumnName("generated_by");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        e.HasOne(x => x.Employee).WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.NoAction);
        e.HasOne(x => x.GeneratedBy).WithMany().HasForeignKey(x => x.GeneratedById).OnDelete(DeleteBehavior.NoAction);
        e.HasIndex(x => x.ReportType);
        e.HasIndex(x => x.CreatedAt);
        e.HasIndex(x => x.GeneratedById);
        e.HasIndex(x => x.OrganizationId);
        e.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        e.HasIndex(x => new { x.OrganizationId, x.ReportType });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> e)
    {
        e.ToTable("audit_logs");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
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
        e.HasIndex(x => new { x.OrganizationId, x.CreatedAt });
        e.HasIndex(x => new { x.OrganizationId, x.EntityType, x.EntityId });
    }
}

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> e)
    {
        e.ToTable("refresh_tokens");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        e.HasIndex(x => x.TokenHash);
        e.Property(x => x.TokenFamilyId).HasColumnName("token_family_id").IsRequired();
        e.HasIndex(x => new { x.TokenFamilyId, x.UserId });
        e.Property(x => x.ReplacedByTokenHash).HasColumnName("replaced_by_token_hash").HasMaxLength(64);
        e.Property(x => x.ReuseDetectedAt).HasColumnName("reuse_detected_at");
        e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        e.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        e.Ignore(x => x.IsActive);
        e.HasIndex(x => new { x.OrganizationId, x.UserId });
    }
}

public class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> e)
    {
        e.ToTable("password_reset_tokens");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64).IsRequired();
        e.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        e.Property(x => x.UsedAt).HasColumnName("used_at");
        e.Property(x => x.RequestIp).HasColumnName("request_ip").HasMaxLength(45);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

public class PlatformModulePermissionConfiguration : IEntityTypeConfiguration<PlatformModulePermission>
{
    public void Configure(EntityTypeBuilder<PlatformModulePermission> e)
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
    }
}

public class ProjectHealthSnapshotConfiguration : IEntityTypeConfiguration<ProjectHealthSnapshot>
{
    public void Configure(EntityTypeBuilder<ProjectHealthSnapshot> e)
    {
        e.ToTable("project_health_snapshots");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.ProjectId).HasColumnName("project_id");
        e.Property(x => x.Score).HasColumnName("score").HasPrecision(5, 2);
        e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.Progress).HasColumnName("progress").HasPrecision(5, 2);
        e.Property(x => x.OnTimeTasks).HasColumnName("on_time_tasks");
        e.Property(x => x.DelayedTasks).HasColumnName("delayed_tasks");
        e.Property(x => x.CriticalRisks).HasColumnName("critical_risks");
        e.Property(x => x.NoteKey).HasColumnName("note_key").HasMaxLength(80);
        e.Property(x => x.CapturedAt).HasColumnName("captured_at");
        e.HasOne(x => x.Project).WithMany().HasForeignKey(x => x.ProjectId);
        e.HasIndex(x => new { x.ProjectId, x.CapturedAt });
    }
}

public class EvaluationPeriodConfiguration : IEntityTypeConfiguration<EvaluationPeriod>
{
    public void Configure(EntityTypeBuilder<EvaluationPeriod> e)
    {
        e.ToTable("evaluation_periods");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        e.Property(x => x.StartDate).HasColumnName("start_date");
        e.Property(x => x.EndDate).HasColumnName("end_date");
        e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
    }
}

public class KpiDefinitionConfiguration : IEntityTypeConfiguration<KpiDefinition>
{
    public void Configure(EntityTypeBuilder<KpiDefinition> e)
    {
        e.ToTable("kpi_definitions");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        e.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
        e.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        e.Property(x => x.Weight).HasColumnName("weight").HasPrecision(8, 2);
        e.Property(x => x.HigherIsBetter).HasColumnName("higher_is_better");
        e.Property(x => x.IsActive).HasColumnName("is_active");
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
    }
}

public class EmployeeEvaluationConfiguration : IEntityTypeConfiguration<EmployeeEvaluation>
{
    public void Configure(EntityTypeBuilder<EmployeeEvaluation> e)
    {
        e.ToTable("employee_evaluations");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.PeriodId).HasColumnName("period_id");
        e.Property(x => x.UserId).HasColumnName("user_id");
        e.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20);
        e.Property(x => x.OverallScore).HasColumnName("overall_score").HasPrecision(8, 2);
        e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(2000);
        e.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
        e.Property(x => x.ReviewedById).HasColumnName("reviewed_by_id");
        e.Property(x => x.ReviewedAt).HasColumnName("reviewed_at");
        e.Property(x => x.ApprovedById).HasColumnName("approved_by_id");
        e.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        e.Property(x => x.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Period).WithMany(p => p.Evaluations).HasForeignKey(x => x.PeriodId);
        e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        e.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.NoAction);
        e.HasOne(x => x.ApprovedBy).WithMany().HasForeignKey(x => x.ApprovedById).OnDelete(DeleteBehavior.NoAction);
    }
}

public class EmployeeKpiResultConfiguration : IEntityTypeConfiguration<EmployeeKpiResult>
{
    public void Configure(EntityTypeBuilder<EmployeeKpiResult> e)
    {
        e.ToTable("employee_kpi_results");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.EvaluationId).HasColumnName("evaluation_id");
        e.Property(x => x.KpiDefinitionId).HasColumnName("kpi_definition_id");
        e.Property(x => x.CalculatedValue).HasColumnName("calculated_value").HasPrecision(12, 4);
        e.Property(x => x.AdjustedValue).HasColumnName("adjusted_value").HasPrecision(12, 4);
        e.Property(x => x.Score).HasColumnName("score").HasPrecision(8, 2);
        e.Property(x => x.Comment).HasColumnName("comment").HasMaxLength(1000);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Evaluation).WithMany(ev => ev.Results).HasForeignKey(x => x.EvaluationId);
        e.HasOne(x => x.KpiDefinition).WithMany().HasForeignKey(x => x.KpiDefinitionId);
    }
}

public class TaskQualityEvaluationConfiguration : IEntityTypeConfiguration<TaskQualityEvaluation>
{
    public void Configure(EntityTypeBuilder<TaskQualityEvaluation> e)
    {
        e.ToTable("task_quality_evaluations");
        e.HasKey(x => x.Id);
        e.Property(x => x.Id).HasColumnName("id");
        e.Property(x => x.OrganizationId).HasColumnName("organization_id");
        e.Property(x => x.TaskId).HasColumnName("task_id");
        e.Property(x => x.EvaluatorId).HasColumnName("evaluator_id");
        e.Property(x => x.QualityScore).HasColumnName("quality_score");
        e.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(1000);
        e.Property(x => x.CreatedAt).HasColumnName("created_at");
        e.Property(x => x.IsDeleted).HasColumnName("is_deleted");
        e.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        e.HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId);
        e.HasOne(x => x.Evaluator).WithMany().HasForeignKey(x => x.EvaluatorId).OnDelete(DeleteBehavior.NoAction);
    }
}
