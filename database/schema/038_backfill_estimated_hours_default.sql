-- ============================================================================
-- 038 — Backfill legacy EstimatedHours: null/0 → 1 (equal unit weight).
-- Policy: never treat missing effort as zero weight (that would zero out project progress).
-- Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tasks', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.tasks', N'estimated_hours') IS NOT NULL
BEGIN
    UPDATE dbo.tasks
    SET estimated_hours = 1
    WHERE estimated_hours IS NULL OR estimated_hours <= 0;

    -- Keep column non-nullable with default 1 for new inserts.
    IF EXISTS (
        SELECT 1 FROM sys.columns
        WHERE object_id = OBJECT_ID(N'dbo.tasks')
          AND name = N'estimated_hours'
          AND is_nullable = 1)
    BEGIN
        UPDATE dbo.tasks SET estimated_hours = 1 WHERE estimated_hours IS NULL;
        ALTER TABLE dbo.tasks ALTER COLUMN estimated_hours DECIMAL(10,2) NOT NULL;
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.default_constraints dc
        JOIN sys.columns c ON c.default_object_id = dc.object_id
        WHERE dc.parent_object_id = OBJECT_ID(N'dbo.tasks') AND c.name = N'estimated_hours')
        ALTER TABLE dbo.tasks ADD CONSTRAINT DF_tasks_estimated_hours DEFAULT (1) FOR estimated_hours;
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'038_backfill_estimated_hours_default.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'038_backfill_estimated_hours_default.sql');
GO

PRINT '038_backfill_estimated_hours_default: completed.';
GO
