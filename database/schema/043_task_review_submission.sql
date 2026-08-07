-- ============================================================================
-- 043 — Task review submission metadata (who/when submitted for review).
-- Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tasks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tasks', N'submitted_for_review_at') IS NULL
        ALTER TABLE dbo.tasks ADD submitted_for_review_at DATETIMEOFFSET NULL;

    IF COL_LENGTH(N'dbo.tasks', N'submitted_for_review_by_id') IS NULL
        ALTER TABLE dbo.tasks ADD submitted_for_review_by_id BIGINT NULL;
END
GO

IF OBJECT_ID(N'dbo.tasks', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.tasks', N'submitted_for_review_by_id') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_tasks_submitted_for_review_by'
   )
BEGIN
    ALTER TABLE dbo.tasks
        ADD CONSTRAINT FK_tasks_submitted_for_review_by
        FOREIGN KEY (submitted_for_review_by_id) REFERENCES dbo.users(id);
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'043_task_review_submission.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'043_task_review_submission.sql');
GO

PRINT '043_task_review_submission: completed.';
GO
