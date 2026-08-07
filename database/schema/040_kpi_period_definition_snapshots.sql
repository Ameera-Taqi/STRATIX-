-- ============================================================================
-- 040 — KPI definition snapshots for evaluation periods + result denormalization.
-- When a period opens, active KPI weight/target/formula/name are frozen.
-- Employee KPI results store the same snapshot so historical scores stay interpretable.
-- Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.kpi_definitions', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.kpi_definitions', N'target_value') IS NULL
        ALTER TABLE dbo.kpi_definitions ADD target_value DECIMAL(12,4) NULL;
    IF COL_LENGTH(N'dbo.kpi_definitions', N'formula') IS NULL
        ALTER TABLE dbo.kpi_definitions ADD formula NVARCHAR(200) NULL;
END
GO

IF OBJECT_ID(N'dbo.period_kpi_snapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.period_kpi_snapshots
    (
        id                  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_period_kpi_snapshots PRIMARY KEY,
        organization_id     BIGINT NOT NULL,
        period_id           BIGINT NOT NULL,
        kpi_definition_id   BIGINT NOT NULL,
        code                NVARCHAR(80) NOT NULL,
        name                NVARCHAR(150) NOT NULL,
        weight              DECIMAL(8,2) NOT NULL CONSTRAINT DF_pks_weight DEFAULT (1),
        target_value        DECIMAL(12,4) NULL,
        formula             NVARCHAR(200) NOT NULL,
        higher_is_better    BIT NOT NULL CONSTRAINT DF_pks_hib DEFAULT (1),
        created_at          DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_pks_created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_pks_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_pks_period FOREIGN KEY (period_id) REFERENCES dbo.evaluation_periods(id),
        CONSTRAINT FK_pks_def FOREIGN KEY (kpi_definition_id) REFERENCES dbo.kpi_definitions(id),
        CONSTRAINT UX_pks_period_def UNIQUE (period_id, kpi_definition_id)
    );
    CREATE INDEX IX_pks_period ON dbo.period_kpi_snapshots (period_id);
END
GO

IF OBJECT_ID(N'dbo.employee_kpi_results', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.employee_kpi_results', N'snapshot_name') IS NULL
        ALTER TABLE dbo.employee_kpi_results ADD snapshot_name NVARCHAR(150) NOT NULL
            CONSTRAINT DF_kpi_res_snap_name DEFAULT (N'');
    IF COL_LENGTH(N'dbo.employee_kpi_results', N'snapshot_code') IS NULL
        ALTER TABLE dbo.employee_kpi_results ADD snapshot_code NVARCHAR(80) NOT NULL
            CONSTRAINT DF_kpi_res_snap_code DEFAULT (N'');
    IF COL_LENGTH(N'dbo.employee_kpi_results', N'snapshot_weight') IS NULL
        ALTER TABLE dbo.employee_kpi_results ADD snapshot_weight DECIMAL(8,2) NOT NULL
            CONSTRAINT DF_kpi_res_snap_weight DEFAULT (1);
    IF COL_LENGTH(N'dbo.employee_kpi_results', N'snapshot_target') IS NULL
        ALTER TABLE dbo.employee_kpi_results ADD snapshot_target DECIMAL(12,4) NULL;
    IF COL_LENGTH(N'dbo.employee_kpi_results', N'snapshot_formula') IS NULL
        ALTER TABLE dbo.employee_kpi_results ADD snapshot_formula NVARCHAR(200) NOT NULL
            CONSTRAINT DF_kpi_res_snap_formula DEFAULT (N'');
    IF COL_LENGTH(N'dbo.employee_kpi_results', N'snapshot_higher_is_better') IS NULL
        ALTER TABLE dbo.employee_kpi_results ADD snapshot_higher_is_better BIT NOT NULL
            CONSTRAINT DF_kpi_res_snap_hib DEFAULT (1);
END
GO

-- Backfill snapshot columns from live definitions where still empty.
IF OBJECT_ID(N'dbo.employee_kpi_results', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.kpi_definitions', N'U') IS NOT NULL
BEGIN
    UPDATE r
    SET
        snapshot_name = d.name,
        snapshot_code = d.code,
        snapshot_weight = CASE WHEN d.weight > 0 THEN d.weight ELSE 1 END,
        snapshot_formula = COALESCE(NULLIF(d.formula, N''), d.code),
        snapshot_higher_is_better = d.higher_is_better,
        snapshot_target = d.target_value
    FROM dbo.employee_kpi_results r
    INNER JOIN dbo.kpi_definitions d ON d.id = r.kpi_definition_id
    WHERE r.is_deleted = 0
      AND (r.snapshot_code = N'' OR r.snapshot_name = N'');
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'040_kpi_period_definition_snapshots.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'040_kpi_period_definition_snapshots.sql');
GO

PRINT '040_kpi_period_definition_snapshots: completed.';
GO
