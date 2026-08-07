-- ============================================================================
-- 044 — Risk closure reason + optional residual risk.
-- Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.project_risks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.project_risks', N'closure_reason') IS NULL
        ALTER TABLE dbo.project_risks ADD closure_reason NVARCHAR(1000) NULL;

    IF COL_LENGTH(N'dbo.project_risks', N'residual_risk') IS NULL
        ALTER TABLE dbo.project_risks ADD residual_risk NVARCHAR(20) NULL;
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'044_risk_closure.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'044_risk_closure.sql');
GO

PRINT '044_risk_closure: completed.';
GO
