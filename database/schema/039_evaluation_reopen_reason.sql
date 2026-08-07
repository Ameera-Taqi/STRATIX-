-- ============================================================================
-- 039 — Employee evaluation reopen reason (formal unlock after APPROVED).
-- Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.employee_evaluations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.employee_evaluations', N'reopen_reason') IS NULL
BEGIN
    ALTER TABLE dbo.employee_evaluations
        ADD reopen_reason NVARCHAR(1000) NULL;
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'039_evaluation_reopen_reason.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'039_evaluation_reopen_reason.sql');
GO

PRINT '039_evaluation_reopen_reason: completed.';
GO
