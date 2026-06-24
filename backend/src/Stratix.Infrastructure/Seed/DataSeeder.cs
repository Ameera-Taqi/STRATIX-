using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stratix.Domain.Entities;
using Stratix.Domain.Enums;
using DomainTaskStatus = Stratix.Domain.Enums.TaskStatus;
using Stratix.Domain.Services;
using Stratix.Infrastructure.Persistence;
using Stratix.Application.Interfaces;

namespace Stratix.Infrastructure.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StratixDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");
        var syncPasswords = configuration.GetValue("Stratix:SyncSeedPasswords", true);

        await EnsureDepartmentsAsync(db, ct);

        if (syncPasswords)
            await SyncSeedPasswordsAsync(db, hasher, ct);

        if (await db.ProjectSet.AnyAsync(ct))
        {
            await SeedMissingStagesAndTasksAsync(db, ct);
            return;
        }

        logger.LogInformation("Seeding Stratix development data...");

        var it = await SaveDepartmentAsync(db, "IT", "Information Technology", ct);
        var product = await SaveDepartmentAsync(db, "Product", "Product Engineering", ct);
        var operations = await SaveDepartmentAsync(db, "Operations", "Operations", ct);
        var finance = await SaveDepartmentAsync(db, "Finance", "Finance", ct);

        var admin = await SaveUserAsync(db, hasher, "Admin", "admin@stratix.local", UserRole.ADMIN, "System Administrator", it.Id, ct);
        var pm = await SaveUserAsync(db, hasher, "Sara Ali", "sara.ali@stratix.local", UserRole.PROJECT_MANAGER, "Project Manager", it.Id, ct);
        var omar = await SaveUserAsync(db, hasher, "Omar Hassan", "omar.hassan@stratix.local", UserRole.TEAM_LEADER, "Team Leader", product.Id, ct);
        var lina = await SaveUserAsync(db, hasher, "Lina Noor", "lina.noor@stratix.local", UserRole.EMPLOYEE, "Employee", operations.Id, ct);
        var khalid = await SaveUserAsync(db, hasher, "Khalid Fahad", "khalid.fahad@stratix.local", UserRole.PROJECT_MANAGER, "Project Manager", finance.Id, ct);

        var erp = await SaveProjectAsync(db, "ERP Rollout", "Enterprise resource planning deployment", ProjectStatus.ACTIVE, ProjectPriority.HIGH,
            new DateOnly(2026, 1, 10), new DateOnly(2026, 7, 15), 72, pm.Id, it.Id, ct);
        var mobile = await SaveProjectAsync(db, "Mobile App v2", "Next generation mobile platform", ProjectStatus.ACTIVE, ProjectPriority.MEDIUM,
            new DateOnly(2026, 2, 1), new DateOnly(2026, 8, 1), 45, omar.Id, product.Id, ct);
        var dataMigration = await SaveProjectAsync(db, "Data Migration", "Legacy data migration to new platform", ProjectStatus.ACTIVE, ProjectPriority.CRITICAL,
            new DateOnly(2025, 11, 1), new DateOnly(2026, 6, 20), 90, lina.Id, operations.Id, ct);
        var securityAudit = await SaveProjectAsync(db, "Security Audit", "Annual security audit and remediation", ProjectStatus.ON_HOLD, ProjectPriority.HIGH,
            new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 30), 30, khalid.Id, it.Id, ct);

        await SaveStageAsync(db, erp.Id, "Discovery", new DateOnly(2026, 1, 10), new DateOnly(2026, 2, 28), StageStatus.DONE, 100, 1, ct);
        var implementation = await SaveStageAsync(db, erp.Id, "Implementation", new DateOnly(2026, 3, 1), new DateOnly(2026, 6, 30), StageStatus.ACTIVE, 65, 2, ct);
        await SaveStageAsync(db, erp.Id, "UAT & Go-live", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 15), StageStatus.PLANNED, 10, 3, ct);

        await SaveRiskAsync(db, erp.Id, pm.Id, "Vendor API instability", "Third-party payroll API has intermittent outages",
            RiskImpact.HIGH, RiskProbability.HIGH, "Establish fallback batch sync and SLA monitoring", RiskStatus.OPEN, ct);
        await SaveRiskAsync(db, erp.Id, admin.Id, "Data migration errors", "Legacy data may not map cleanly to new schema",
            RiskImpact.HIGH, RiskProbability.MEDIUM, "Run staged migration with validation scripts", RiskStatus.MITIGATING, ct);
        await SaveRiskAsync(db, mobile.Id, pm.Id, "Scope creep", "Stakeholders requesting additional features mid-sprint",
            RiskImpact.MEDIUM, RiskProbability.MEDIUM, "Change control board review for all new requests", RiskStatus.OPEN, ct);
        await SaveRiskAsync(db, mobile.Id, pm.Id, "App store rejection", "Policy compliance issues during submission",
            RiskImpact.MEDIUM, RiskProbability.LOW, "Pre-submission compliance checklist", RiskStatus.CLOSED, ct);
        await SaveRiskAsync(db, erp.Id, admin.Id, "Training adoption lag", "End users slow to adopt new workflows",
            RiskImpact.LOW, RiskProbability.LOW, "Phased training program with champions", RiskStatus.CLOSED, ct);

        await SaveTaskAsync(db, erp.Id, implementation.Id, "API integration", "Connect payroll module to core ERP.",
            DomainTaskStatus.TODO, TaskPriority.HIGH, pm.Id, new DateOnly(2026, 6, 10), ct);
        await SaveTaskAsync(db, mobile.Id, null, "UI mockups", null, DomainTaskStatus.IN_PROGRESS, TaskPriority.MEDIUM, omar.Id, new DateOnly(2026, 6, 5), ct);
        await SaveTaskAsync(db, dataMigration.Id, null, "Schema validation", null, DomainTaskStatus.REVIEW, TaskPriority.URGENT, lina.Id, new DateOnly(2026, 6, 8), ct);
        await SaveTaskAsync(db, erp.Id, implementation.Id, "Deploy staging", null, DomainTaskStatus.DONE, TaskPriority.LOW, pm.Id, new DateOnly(2026, 6, 12), ct);
        await SaveTaskAsync(db, securityAudit.Id, null, "Pen test report", null, DomainTaskStatus.IN_PROGRESS, TaskPriority.HIGH, khalid.Id, new DateOnly(2026, 6, 15), ct);
    }

    private static async Task EnsureDepartmentsAsync(StratixDbContext db, CancellationToken ct)
    {
        if (!await db.DepartmentSet.AnyAsync(d => d.Name == "HR", ct))
        {
            db.DepartmentSet.Add(new Department { Name = "HR", Description = "Human Resources", CreatedAt = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync(ct);
        }
    }

    private static async Task SyncSeedPasswordsAsync(StratixDbContext db, IPasswordHasher hasher, CancellationToken ct)
    {
        var seeds = new Dictionary<string, string>
        {
            ["admin@stratix.local"] = "1234",
            ["sara.ali@stratix.local"] = "1234",
            ["omar.hassan@stratix.local"] = "1234",
            ["lina.noor@stratix.local"] = "1234",
            ["khalid.fahad@stratix.local"] = "1234"
        };
        foreach (var (email, password) in seeds)
        {
            var user = await db.UserSet.FirstOrDefaultAsync(u => u.Email == email, ct);
            if (user != null)
            {
                user.Password = hasher.Hash(password);
                user.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task SeedMissingStagesAndTasksAsync(StratixDbContext db, CancellationToken ct)
    {
        if (await db.ProjectStageSet.AnyAsync(ct) && await db.TaskSet.AnyAsync(ct)) return;
        var erp = await db.ProjectSet.FirstOrDefaultAsync(p => p.Name == "ERP Rollout", ct);
        if (erp == null) return;
        // Minimal backfill omitted for brevity when partial data exists
    }

    private static async Task<Department> SaveDepartmentAsync(StratixDbContext db, string name, string description, CancellationToken ct)
    {
        var dept = new Department { Name = name, Description = description, CreatedAt = DateTimeOffset.UtcNow };
        db.DepartmentSet.Add(dept);
        await db.SaveChangesAsync(ct);
        return dept;
    }

    private static async Task<User> SaveUserAsync(StratixDbContext db, IPasswordHasher hasher, string name, string email, UserRole role, string jobTitle, long deptId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var user = new User
        {
            Name = name, Email = email, Password = hasher.Hash("1234"), Role = role,
            JobTitle = jobTitle, Status = UserStatus.ACTIVE, DepartmentId = deptId,
            CreatedAt = now, UpdatedAt = now
        };
        db.UserSet.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private static async Task<Project> SaveProjectAsync(StratixDbContext db, string name, string description, ProjectStatus status, ProjectPriority priority,
        DateOnly start, DateOnly end, decimal progress, long managerId, long deptId, CancellationToken ct)
    {
        var project = new Project
        {
            Name = name, Description = description, Status = status, Priority = priority,
            StartDate = start, EndDate = end, Progress = progress,
            ProjectManagerId = managerId, DepartmentId = deptId, CreatedAt = DateTimeOffset.UtcNow
        };
        db.ProjectSet.Add(project);
        await db.SaveChangesAsync(ct);
        return project;
    }

    private static async Task<ProjectStage> SaveStageAsync(StratixDbContext db, long projectId, string name, DateOnly start, DateOnly end, StageStatus status, decimal progress, int order, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var stage = new ProjectStage
        {
            ProjectId = projectId, Name = name, StartDate = start, EndDate = end,
            Status = status, Progress = progress, OrderNumber = order, CreatedAt = now, UpdatedAt = now
        };
        db.ProjectStageSet.Add(stage);
        await db.SaveChangesAsync(ct);
        return stage;
    }

    private static async Task SaveRiskAsync(StratixDbContext db, long projectId, long ownerId, string title, string description,
        RiskImpact impact, RiskProbability probability, string mitigation, RiskStatus status, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        db.ProjectRiskSet.Add(new ProjectRisk
        {
            Title = title, Description = description, Impact = impact, Probability = probability,
            RiskLevel = RiskLevelCalculator.Calculate(impact, probability), MitigationPlan = mitigation,
            Status = status, ProjectId = projectId, OwnerId = ownerId, CreatedAt = now, UpdatedAt = now
        });
        await db.SaveChangesAsync(ct);
    }

    private static async Task SaveTaskAsync(StratixDbContext db, long projectId, long? stageId, string title, string? description,
        DomainTaskStatus status, TaskPriority priority, long assigneeId, DateOnly dueDate, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        db.TaskSet.Add(new TaskItem
        {
            ProjectId = projectId, StageId = stageId, Title = title, Description = description,
            Status = status, Priority = priority, AssigneeId = assigneeId, DueDate = dueDate,
            CreatedAt = now, UpdatedAt = now
        });
        await db.SaveChangesAsync(ct);
    }
}
