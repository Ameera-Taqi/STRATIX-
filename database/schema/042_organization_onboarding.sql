-- ============================================================================
-- 042 — Organization onboarding profile fields.
-- Idempotent. Existing orgs are marked complete so only new tenants enter the wizard.
-- Each ADD COLUMN that later statements reference must be its own batch (GO).
-- Use DATETIME2 to match organizations.created_at / updated_at (EF DateTimeOffset ↔ datetime2).
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.organizations', N'industry') IS NULL
    ALTER TABLE dbo.organizations ADD industry NVARCHAR(100) NULL;
GO

IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.organizations', N'timezone') IS NULL
    ALTER TABLE dbo.organizations ADD timezone NVARCHAR(100) NULL;
GO

IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.organizations', N'preferred_language') IS NULL
    ALTER TABLE dbo.organizations ADD preferred_language NVARCHAR(10) NULL;
GO

IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.organizations', N'onboarding_completed_at') IS NULL
    ALTER TABLE dbo.organizations ADD onboarding_completed_at DATETIME2(0) NULL;
GO

-- If an earlier attempt created DATETIMEOFFSET, normalize to DATETIME2.
IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.organizations', N'onboarding_completed_at') IS NOT NULL
   AND EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_SCHEMA = N'dbo' AND TABLE_NAME = N'organizations'
          AND COLUMN_NAME = N'onboarding_completed_at' AND DATA_TYPE = N'datetimeoffset')
BEGIN
    ALTER TABLE dbo.organizations ALTER COLUMN onboarding_completed_at DATETIME2(0) NULL;
END
GO

-- Existing tenants already use the product — do not force the wizard.
IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.organizations', N'onboarding_completed_at') IS NOT NULL
BEGIN
    UPDATE dbo.organizations
    SET onboarding_completed_at = ISNULL(created_at, SYSUTCDATETIME())
    WHERE onboarding_completed_at IS NULL;
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'042_organization_onboarding.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'042_organization_onboarding.sql');
GO

PRINT '042_organization_onboarding: completed.';
GO
