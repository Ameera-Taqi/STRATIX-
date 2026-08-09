-- ============================================================================
-- 041 — KPI weight rules: applies_to_role scope (sum-to-100% enforced in app).
-- Idempotent.
-- Note: each ALTER that later columns/indexes depend on must be its own batch (GO),
-- otherwise SQL Server still sees the old schema at compile time.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kpi_definitions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.kpi_definitions', N'applies_to_role') IS NULL
BEGIN
    ALTER TABLE dbo.kpi_definitions ADD applies_to_role NVARCHAR(50) NULL;
END
GO

IF OBJECT_ID(N'dbo.kpi_definitions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.kpi_definitions', N'applies_to_role') IS NOT NULL
   AND COL_LENGTH(N'dbo.kpi_definitions', N'applies_to_role_key') IS NULL
BEGIN
    ALTER TABLE dbo.kpi_definitions ADD applies_to_role_key
        AS (CONVERT(NVARCHAR(50), ISNULL(applies_to_role, N'*'))) PERSISTED;
END
GO

IF OBJECT_ID(N'dbo.kpi_definitions', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.kpi_definitions', N'applies_to_role_key') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_kpi_definitions_org_code' AND object_id = OBJECT_ID(N'dbo.kpi_definitions'))
        DROP INDEX UX_kpi_definitions_org_code ON dbo.kpi_definitions;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_kpi_definitions_org_code_role' AND object_id = OBJECT_ID(N'dbo.kpi_definitions'))
        CREATE UNIQUE INDEX UX_kpi_definitions_org_code_role
            ON dbo.kpi_definitions (organization_id, code, applies_to_role_key)
            WHERE is_deleted = 0;
END
GO

IF OBJECT_ID(N'dbo.period_kpi_snapshots', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.period_kpi_snapshots', N'applies_to_role') IS NULL
BEGIN
    ALTER TABLE dbo.period_kpi_snapshots ADD applies_to_role NVARCHAR(50) NULL;
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'041_kpi_weight_total_and_role_scope.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'041_kpi_weight_total_and_role_scope.sql');
GO

PRINT '041_kpi_weight_total_and_role_scope: completed.';
GO
