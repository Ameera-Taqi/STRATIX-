-- ============================================================================
-- 029 — Drop global departments.name unique; sync plan denormalization;
--       add organization_id to refresh_tokens / password_reset_tokens / reports.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- 1) Remove global UNIQUE on departments.name (multi-tenant safe: org+name only)
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_departments_name' AND parent_object_id = OBJECT_ID(N'departments'))
    ALTER TABLE departments DROP CONSTRAINT UQ_departments_name;
GO

-- Ensure per-tenant unique name for active rows (idempotent)
IF OBJECT_ID(N'departments', N'U') IS NOT NULL
   AND COL_LENGTH(N'departments', N'organization_id') IS NOT NULL
   AND COL_LENGTH(N'departments', N'is_deleted') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_departments_org_name_active' AND object_id = OBJECT_ID(N'departments'))
BEGIN
    CREATE UNIQUE INDEX UX_departments_org_name_active
        ON departments (organization_id, name)
        WHERE is_deleted = 0;
END;
GO

-- 2) Source of truth: subscriptions.plan_code → mirror onto organizations.subscription_plan
--    Map TRIAL → PRO so the denormalized enum stays within FREE/PRO/ENTERPRISE.
IF OBJECT_ID(N'dbo.subscriptions', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.organizations', N'U') IS NOT NULL
BEGIN
    UPDATE o
    SET o.subscription_plan = CASE UPPER(LTRIM(RTRIM(s.plan_code)))
            WHEN N'ENTERPRISE' THEN N'ENTERPRISE'
            WHEN N'PRO' THEN N'PRO'
            WHEN N'TRIAL' THEN N'PRO'
            WHEN N'FREE' THEN N'FREE'
            ELSE N'FREE'
        END,
        o.updated_at = SYSUTCDATETIME()
    FROM dbo.organizations o
    INNER JOIN dbo.subscriptions s ON s.organization_id = o.id
    WHERE o.subscription_plan <> CASE UPPER(LTRIM(RTRIM(s.plan_code)))
            WHEN N'ENTERPRISE' THEN N'ENTERPRISE'
            WHEN N'PRO' THEN N'PRO'
            WHEN N'TRIAL' THEN N'PRO'
            WHEN N'FREE' THEN N'FREE'
            ELSE N'FREE'
        END;
END;
GO

-- 3) Tenant columns on token tables
IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.refresh_tokens', N'organization_id') IS NULL
BEGIN
    ALTER TABLE dbo.refresh_tokens ADD organization_id BIGINT NULL;
END;
GO

IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.refresh_tokens', N'organization_id') IS NOT NULL
BEGIN
    UPDATE rt SET organization_id = u.organization_id
    FROM dbo.refresh_tokens rt
    INNER JOIN dbo.users u ON u.id = rt.user_id
    WHERE rt.organization_id IS NULL;

    DECLARE @defaultOrg BIGINT = (SELECT TOP 1 id FROM dbo.organizations WHERE slug = N'default' ORDER BY id);
    IF @defaultOrg IS NULL SET @defaultOrg = (SELECT TOP 1 id FROM dbo.organizations ORDER BY id);
    IF @defaultOrg IS NOT NULL
        UPDATE dbo.refresh_tokens SET organization_id = @defaultOrg WHERE organization_id IS NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.refresh_tokens WHERE organization_id IS NULL)
    BEGIN
        ALTER TABLE dbo.refresh_tokens ALTER COLUMN organization_id BIGINT NOT NULL;

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_refresh_tokens_organization')
            ALTER TABLE dbo.refresh_tokens ADD CONSTRAINT fk_refresh_tokens_organization
                FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_refresh_tokens_organization_id' AND object_id = OBJECT_ID(N'dbo.refresh_tokens'))
            CREATE INDEX ix_refresh_tokens_organization_id ON dbo.refresh_tokens(organization_id);
    END
END;
GO

IF OBJECT_ID(N'dbo.password_reset_tokens', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.password_reset_tokens', N'organization_id') IS NULL
BEGIN
    ALTER TABLE dbo.password_reset_tokens ADD organization_id BIGINT NULL;
END;
GO

IF OBJECT_ID(N'dbo.password_reset_tokens', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.password_reset_tokens', N'organization_id') IS NOT NULL
BEGIN
    UPDATE prt SET organization_id = u.organization_id
    FROM dbo.password_reset_tokens prt
    INNER JOIN dbo.users u ON u.id = prt.user_id
    WHERE prt.organization_id IS NULL;

    DECLARE @defaultOrg2 BIGINT = (SELECT TOP 1 id FROM dbo.organizations WHERE slug = N'default' ORDER BY id);
    IF @defaultOrg2 IS NULL SET @defaultOrg2 = (SELECT TOP 1 id FROM dbo.organizations ORDER BY id);
    IF @defaultOrg2 IS NOT NULL
        UPDATE dbo.password_reset_tokens SET organization_id = @defaultOrg2 WHERE organization_id IS NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.password_reset_tokens WHERE organization_id IS NULL)
    BEGIN
        ALTER TABLE dbo.password_reset_tokens ALTER COLUMN organization_id BIGINT NOT NULL;

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_password_reset_tokens_organization')
            ALTER TABLE dbo.password_reset_tokens ADD CONSTRAINT fk_password_reset_tokens_organization
                FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_password_reset_tokens_organization_id' AND object_id = OBJECT_ID(N'dbo.password_reset_tokens'))
            CREATE INDEX ix_password_reset_tokens_organization_id ON dbo.password_reset_tokens(organization_id);
    END
END;
GO

-- Optional reports table (010) — tenant-scope if present
IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.reports', N'organization_id') IS NULL
BEGIN
    ALTER TABLE dbo.reports ADD organization_id BIGINT NULL;
END;
GO

IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.reports', N'organization_id') IS NOT NULL
BEGIN
    UPDATE r SET organization_id = COALESCE(p.organization_id, d.organization_id, u.organization_id)
    FROM dbo.reports r
    LEFT JOIN dbo.projects p ON p.id = r.project_id
    LEFT JOIN dbo.departments d ON d.id = r.department_id
    LEFT JOIN dbo.users u ON u.id = r.generated_by
    WHERE r.organization_id IS NULL;

    DECLARE @defaultOrg3 BIGINT = (SELECT TOP 1 id FROM dbo.organizations WHERE slug = N'default' ORDER BY id);
    IF @defaultOrg3 IS NULL SET @defaultOrg3 = (SELECT TOP 1 id FROM dbo.organizations ORDER BY id);
    IF @defaultOrg3 IS NOT NULL
        UPDATE dbo.reports SET organization_id = @defaultOrg3 WHERE organization_id IS NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.reports WHERE organization_id IS NULL)
    BEGIN
        ALTER TABLE dbo.reports ALTER COLUMN organization_id BIGINT NOT NULL;

        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_reports_organization')
            ALTER TABLE dbo.reports ADD CONSTRAINT fk_reports_organization
                FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_reports_organization_id' AND object_id = OBJECT_ID(N'dbo.reports'))
            CREATE INDEX ix_reports_organization_id ON dbo.reports(organization_id);
    END
END;
GO

PRINT '029_plan_truth_and_token_org: completed.';
GO
