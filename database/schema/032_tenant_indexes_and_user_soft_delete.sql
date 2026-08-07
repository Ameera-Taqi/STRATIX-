-- ============================================================================
-- 032 — Composite tenant indexes + soft-delete for users.
-- Projects already soft-delete via ISoftDeletable (028).
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ---------------------------------------------------------------------------
-- Users soft delete
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.users', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.users', N'is_deleted') IS NULL
BEGIN
    ALTER TABLE dbo.users ADD is_deleted BIT NOT NULL CONSTRAINT DF_users_is_deleted DEFAULT 0;
    ALTER TABLE dbo.users ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

-- Prefer tenant-scoped unique email for active users (drop global unique if present)
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_users_email' AND parent_object_id = OBJECT_ID(N'dbo.users'))
    ALTER TABLE dbo.users DROP CONSTRAINT UQ_users_email;
GO
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_users_Email' AND object_id = OBJECT_ID(N'dbo.users') AND is_unique = 1)
    DROP INDEX IX_users_Email ON dbo.users;
GO

IF OBJECT_ID(N'dbo.users', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.users', N'is_deleted') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_users_org_email_active' AND object_id = OBJECT_ID(N'dbo.users'))
BEGIN
    CREATE UNIQUE INDEX UX_users_org_email_active
        ON dbo.users (organization_id, email)
        WHERE is_deleted = 0;
END;
GO

-- ---------------------------------------------------------------------------
-- Composite tenant indexes (idempotent)
-- ---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.projects', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_projects_org_status' AND object_id = OBJECT_ID(N'dbo.projects'))
        CREATE INDEX IX_projects_org_status ON dbo.projects (organization_id, status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_projects_org_department' AND object_id = OBJECT_ID(N'dbo.projects'))
        CREATE INDEX IX_projects_org_department ON dbo.projects (organization_id, department_id);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_projects_org_manager' AND object_id = OBJECT_ID(N'dbo.projects'))
        CREATE INDEX IX_projects_org_manager ON dbo.projects (organization_id, project_manager_id);
END;
GO

IF OBJECT_ID(N'dbo.tasks', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tasks_org_project' AND object_id = OBJECT_ID(N'dbo.tasks'))
        CREATE INDEX IX_tasks_org_project ON dbo.tasks (organization_id, project_id);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tasks_org_status' AND object_id = OBJECT_ID(N'dbo.tasks'))
        CREATE INDEX IX_tasks_org_status ON dbo.tasks (organization_id, status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_tasks_org_assignee' AND object_id = OBJECT_ID(N'dbo.tasks'))
        CREATE INDEX IX_tasks_org_assignee ON dbo.tasks (organization_id, assignee_id);
END;
GO

IF OBJECT_ID(N'dbo.project_stages', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_project_stages_org_project' AND object_id = OBJECT_ID(N'dbo.project_stages'))
        CREATE INDEX IX_project_stages_org_project ON dbo.project_stages (organization_id, project_id);
END;
GO

IF OBJECT_ID(N'dbo.project_risks', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_project_risks_org_project' AND object_id = OBJECT_ID(N'dbo.project_risks'))
        CREATE INDEX IX_project_risks_org_project ON dbo.project_risks (organization_id, project_id);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_project_risks_org_status' AND object_id = OBJECT_ID(N'dbo.project_risks'))
        CREATE INDEX IX_project_risks_org_status ON dbo.project_risks (organization_id, status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_project_risks_org_owner' AND object_id = OBJECT_ID(N'dbo.project_risks'))
        CREATE INDEX IX_project_risks_org_owner ON dbo.project_risks (organization_id, owner_id);
END;
GO

IF OBJECT_ID(N'dbo.project_files', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_project_files_org_project' AND object_id = OBJECT_ID(N'dbo.project_files'))
        CREATE INDEX IX_project_files_org_project ON dbo.project_files (organization_id, project_id);
END;
GO

IF OBJECT_ID(N'dbo.task_comments', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_task_comments_org_task' AND object_id = OBJECT_ID(N'dbo.task_comments'))
        CREATE INDEX IX_task_comments_org_task ON dbo.task_comments (organization_id, task_id);
END;
GO

IF OBJECT_ID(N'dbo.notifications', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_notifications_org_user_read' AND object_id = OBJECT_ID(N'dbo.notifications'))
        CREATE INDEX IX_notifications_org_user_read ON dbo.notifications (organization_id, user_id, is_read);
END;
GO

IF OBJECT_ID(N'dbo.users', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_users_org_status' AND object_id = OBJECT_ID(N'dbo.users'))
        CREATE INDEX IX_users_org_status ON dbo.users (organization_id, status);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_users_org_department' AND object_id = OBJECT_ID(N'dbo.users'))
        CREATE INDEX IX_users_org_department ON dbo.users (organization_id, department_id);
END;
GO

IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_reports_org_created' AND object_id = OBJECT_ID(N'dbo.reports'))
        CREATE INDEX IX_reports_org_created ON dbo.reports (organization_id, created_at DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_reports_org_type' AND object_id = OBJECT_ID(N'dbo.reports'))
        CREATE INDEX IX_reports_org_type ON dbo.reports (organization_id, report_type);
END;
GO

IF OBJECT_ID(N'dbo.audit_logs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_audit_logs_org_created' AND object_id = OBJECT_ID(N'dbo.audit_logs'))
        CREATE INDEX IX_audit_logs_org_created ON dbo.audit_logs (organization_id, created_at DESC);
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_audit_logs_org_entity' AND object_id = OBJECT_ID(N'dbo.audit_logs'))
        CREATE INDEX IX_audit_logs_org_entity ON dbo.audit_logs (organization_id, entity_type, entity_id);
END;
GO

IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_refresh_tokens_org_user' AND object_id = OBJECT_ID(N'dbo.refresh_tokens'))
        CREATE INDEX IX_refresh_tokens_org_user ON dbo.refresh_tokens (organization_id, user_id);
END;
GO

PRINT '032_tenant_indexes_and_user_soft_delete: completed.';
GO
