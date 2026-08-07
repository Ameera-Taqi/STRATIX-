-- ============================================================================
-- 042 — Organization onboarding profile fields.
-- Idempotent. Existing orgs are marked complete so only new tenants enter the wizard.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.organizations', N'industry') IS NULL
        ALTER TABLE dbo.organizations ADD industry NVARCHAR(100) NULL;

    IF COL_LENGTH(N'dbo.organizations', N'timezone') IS NULL
        ALTER TABLE dbo.organizations ADD timezone NVARCHAR(100) NULL;

    IF COL_LENGTH(N'dbo.organizations', N'preferred_language') IS NULL
        ALTER TABLE dbo.organizations ADD preferred_language NVARCHAR(10) NULL;

    IF COL_LENGTH(N'dbo.organizations', N'onboarding_completed_at') IS NULL
        ALTER TABLE dbo.organizations ADD onboarding_completed_at DATETIMEOFFSET NULL;

    -- Existing tenants already use the product — do not force the wizard.
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
