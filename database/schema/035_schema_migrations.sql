-- ============================================================================
-- 035 — Schema migration tracking for readiness checks. Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.schema_migrations (
        migration_id NVARCHAR(128) NOT NULL CONSTRAINT pk_schema_migrations PRIMARY KEY,
        applied_at   DATETIME2(0)  NOT NULL CONSTRAINT df_schema_migrations_applied DEFAULT SYSUTCDATETIME()
    );
END
GO

-- Backfill for databases already upgraded past soft-delete + refresh family (pre-tracking).
IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.refresh_tokens', N'token_family_id') IS NOT NULL
BEGIN
    DECLARE @ids TABLE (migration_id NVARCHAR(128) NOT NULL PRIMARY KEY);
    INSERT INTO @ids (migration_id) VALUES
        (N'001_departments.sql'),
        (N'002_users.sql'),
        (N'003_projects.sql'),
        (N'004_project_stages.sql'),
        (N'005_tasks.sql'),
        (N'006_task_comments.sql'),
        (N'010_reports.sql'),
        (N'011_project_risks.sqlserver.sql'),
        (N'012_status_lookups.sqlserver.sql'),
        (N'014_drop_project_milestones.sqlserver.sql'),
        (N'015_password_reset_tokens.sqlserver.sql'),
        (N'016_multitenancy.sqlserver.sql'),
        (N'017_subscription_plans.sqlserver.sql'),
        (N'018_subscriptions.sqlserver.sql'),
        (N'019_refresh_tokens.sqlserver.sql'),
        (N'020_login_lockout.sqlserver.sql'),
        (N'021_wave2_entities.sqlserver.sql'),
        (N'022_task_comments_org.sql'),
        (N'023_platform_module_permissions.sql'),
        (N'024_organization_logo.sql'),
        (N'025_organization_roles.sql'),
        (N'026_drop_change_requests.sql'),
        (N'027_project_file_details.sql'),
        (N'028_soft_delete.sql'),
        (N'029_plan_truth_and_token_org.sql'),
        (N'030_reports_module.sql'),
        (N'031_employee_kpi_unique_period.sql'),
        (N'032_tenant_indexes_and_user_soft_delete.sql'),
        (N'033_global_unique_email.sql'),
        (N'034_refresh_token_reuse_detection.sql'),
        (N'035_schema_migrations.sql');

    INSERT INTO dbo.schema_migrations (migration_id)
    SELECT i.migration_id
    FROM @ids i
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.schema_migrations m WHERE m.migration_id = i.migration_id
    );
END
GO

PRINT '035_schema_migrations: completed.';
GO
