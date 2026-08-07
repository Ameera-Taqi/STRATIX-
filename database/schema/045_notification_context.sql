-- ============================================================================
-- 045 — Notification context for clickable Notification Center.
-- Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.notifications', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.notifications', N'actor_name') IS NULL
        ALTER TABLE dbo.notifications ADD actor_name NVARCHAR(200) NULL;

    IF COL_LENGTH(N'dbo.notifications', N'project_name') IS NULL
        ALTER TABLE dbo.notifications ADD project_name NVARCHAR(200) NULL;

    IF COL_LENGTH(N'dbo.notifications', N'entity_type') IS NULL
        ALTER TABLE dbo.notifications ADD entity_type NVARCHAR(40) NULL;

    IF COL_LENGTH(N'dbo.notifications', N'entity_id') IS NULL
        ALTER TABLE dbo.notifications ADD entity_id BIGINT NULL;

    IF COL_LENGTH(N'dbo.notifications', N'entity_label') IS NULL
        ALTER TABLE dbo.notifications ADD entity_label NVARCHAR(300) NULL;
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'045_notification_context.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'045_notification_context.sql');
GO

PRINT '045_notification_context: completed.';
GO
