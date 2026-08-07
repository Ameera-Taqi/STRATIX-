-- ============================================================================
-- 031 — Unique KPI per employee period within a tenant.
-- UNIQUE (organization_id, user_id, period) for active rows only (soft-delete aware).
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.employee_kpis', N'U') IS NULL
BEGIN
    PRINT '031_employee_kpi_unique_period: employee_kpis missing — skipped.';
END
ELSE
BEGIN
    -- Ensure soft-delete column exists (normally from 028)
    IF COL_LENGTH(N'dbo.employee_kpis', N'is_deleted') IS NULL
    BEGIN
        ALTER TABLE dbo.employee_kpis ADD is_deleted BIT NOT NULL CONSTRAINT DF_employee_kpis_is_deleted_031 DEFAULT 0;
        ALTER TABLE dbo.employee_kpis ADD deleted_at DATETIMEOFFSET NULL;
    END
END
GO

-- Deduplicate before creating unique index: keep newest id per (org, user, period)
IF OBJECT_ID(N'dbo.employee_kpis', N'U') IS NOT NULL
BEGIN
    ;WITH ranked AS (
        SELECT id,
               ROW_NUMBER() OVER (
                   PARTITION BY organization_id, user_id, period
                   ORDER BY updated_at DESC, id DESC
               ) AS rn
        FROM dbo.employee_kpis
        WHERE is_deleted = 0
    )
    UPDATE k
    SET is_deleted = 1,
        deleted_at = SYSUTCDATETIME()
    FROM dbo.employee_kpis k
    INNER JOIN ranked r ON r.id = k.id
    WHERE r.rn > 1;
END
GO

IF OBJECT_ID(N'dbo.employee_kpis', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'UX_employee_kpis_org_user_period_active'
          AND object_id = OBJECT_ID(N'dbo.employee_kpis')
   )
BEGIN
    CREATE UNIQUE INDEX UX_employee_kpis_org_user_period_active
        ON dbo.employee_kpis (organization_id, user_id, period)
        WHERE is_deleted = 0;
END
GO

PRINT '031_employee_kpi_unique_period: completed.';
GO
