-- ============================================================================
-- 030 — Activate reports module: modernize columns for EF Report entity + soft delete.
-- Idempotent upgrade from legacy 010 (file_url / generated_by only).
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.reports', N'U') IS NULL
BEGIN
    -- Greenfield should have created via 010; create modern table if somehow missing.
    CREATE TABLE dbo.reports (
        id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_reports PRIMARY KEY,
        organization_id  BIGINT        NOT NULL,
        title            NVARCHAR(300) NOT NULL,
        report_type      NVARCHAR(50)  NOT NULL,
        format           NVARCHAR(20)  NOT NULL CONSTRAINT DF_reports_format_030 DEFAULT N'PDF',
        project_id       BIGINT        NULL,
        department_id    BIGINT        NULL,
        employee_id      BIGINT        NULL,
        date_from        DATE          NULL,
        date_to          DATE          NULL,
        file_name        NVARCHAR(300) NOT NULL,
        storage_key      NVARCHAR(500) NOT NULL,
        content_type     NVARCHAR(150) NULL,
        size_bytes       BIGINT        NOT NULL CONSTRAINT DF_reports_size_030 DEFAULT 0,
        generated_by     BIGINT        NOT NULL,
        created_at       DATETIME2(0)  NOT NULL CONSTRAINT DF_reports_created_030 DEFAULT SYSUTCDATETIME(),
        is_deleted       BIT           NOT NULL CONSTRAINT DF_reports_is_deleted_030 DEFAULT 0,
        deleted_at       DATETIMEOFFSET NULL,
        CONSTRAINT fk_reports_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_reports_generator FOREIGN KEY (generated_by) REFERENCES dbo.users(id)
    );
    CREATE INDEX ix_reports_organization_id ON dbo.reports(organization_id);
END
GO

-- organization_id (may already exist from 029)
IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.reports', N'organization_id') IS NULL
    ALTER TABLE dbo.reports ADD organization_id BIGINT NULL;
GO

IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.reports', N'organization_id') IS NOT NULL
BEGIN
    UPDATE r SET organization_id = COALESCE(p.organization_id, d.organization_id, u.organization_id)
    FROM dbo.reports r
    LEFT JOIN dbo.projects p ON p.id = r.project_id
    LEFT JOIN dbo.departments d ON d.id = r.department_id
    LEFT JOIN dbo.users u ON u.id = r.generated_by
    WHERE r.organization_id IS NULL;

    DECLARE @org BIGINT = (SELECT TOP 1 id FROM dbo.organizations WHERE slug = N'default' ORDER BY id);
    IF @org IS NULL SET @org = (SELECT TOP 1 id FROM dbo.organizations ORDER BY id);
    IF @org IS NOT NULL
        UPDATE dbo.reports SET organization_id = @org WHERE organization_id IS NULL;

    IF NOT EXISTS (SELECT 1 FROM dbo.reports WHERE organization_id IS NULL)
    BEGIN
        ALTER TABLE dbo.reports ALTER COLUMN organization_id BIGINT NOT NULL;
        IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_reports_organization')
            ALTER TABLE dbo.reports ADD CONSTRAINT fk_reports_organization
                FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_reports_organization_id' AND object_id = OBJECT_ID(N'dbo.reports'))
            CREATE INDEX ix_reports_organization_id ON dbo.reports(organization_id);
    END
END
GO

-- file_name / storage_key / content_type / size_bytes (replace legacy file_url)
IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.reports', N'file_name') IS NULL
        ALTER TABLE dbo.reports ADD file_name NVARCHAR(300) NULL;

    IF COL_LENGTH(N'dbo.reports', N'storage_key') IS NULL
        ALTER TABLE dbo.reports ADD storage_key NVARCHAR(500) NULL;

    IF COL_LENGTH(N'dbo.reports', N'content_type') IS NULL
        ALTER TABLE dbo.reports ADD content_type NVARCHAR(150) NULL;

    IF COL_LENGTH(N'dbo.reports', N'size_bytes') IS NULL
        ALTER TABLE dbo.reports ADD size_bytes BIGINT NOT NULL CONSTRAINT DF_reports_size_upgrade DEFAULT 0;
END
GO

IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.reports', N'file_url') IS NOT NULL
BEGIN
    UPDATE dbo.reports
    SET file_name = COALESCE(NULLIF(LTRIM(RTRIM(file_name)), N''), N'legacy-report.bin'),
        storage_key = COALESCE(NULLIF(LTRIM(RTRIM(storage_key)), N''), CONCAT(N'legacy/', CAST(id AS NVARCHAR(20)))),
        content_type = COALESCE(content_type, N'application/octet-stream')
    WHERE file_name IS NULL OR storage_key IS NULL;

    -- Drop legacy column after backfill
    ALTER TABLE dbo.reports DROP COLUMN file_url;
END
GO

IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL
BEGIN
    UPDATE dbo.reports SET file_name = N'report.bin' WHERE file_name IS NULL OR LTRIM(RTRIM(file_name)) = N'';
    UPDATE dbo.reports SET storage_key = CONCAT(N'legacy/', CAST(id AS NVARCHAR(20))) WHERE storage_key IS NULL OR LTRIM(RTRIM(storage_key)) = N'';

    ALTER TABLE dbo.reports ALTER COLUMN file_name NVARCHAR(300) NOT NULL;
    ALTER TABLE dbo.reports ALTER COLUMN storage_key NVARCHAR(500) NOT NULL;
END
GO

-- Soft delete
IF OBJECT_ID(N'dbo.reports', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.reports', N'is_deleted') IS NULL
BEGIN
    ALTER TABLE dbo.reports ADD is_deleted BIT NOT NULL CONSTRAINT DF_reports_is_deleted DEFAULT 0;
    ALTER TABLE dbo.reports ADD deleted_at DATETIMEOFFSET NULL;
END
GO

PRINT '030_reports_module: completed.';
GO
