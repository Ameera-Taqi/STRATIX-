-- ============================================================================
-- 021 — Wave 2 entities: employee_kpis, notifications, project_files
-- Creates modern tables on greenfield OR upgrades legacy v1 shapes (007/008/009).
-- Idempotent. Soft-delete columns are added later by 028_soft_delete.sql.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

DECLARE @defaultOrg BIGINT = (SELECT TOP 1 id FROM dbo.organizations WHERE slug = N'default' ORDER BY id);
IF @defaultOrg IS NULL
    SET @defaultOrg = (SELECT TOP 1 id FROM dbo.organizations ORDER BY id);
GO

-- ---------------------------------------------------------------------------
-- employee_kpis
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.employee_kpis', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.employee_kpis (
        id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_employee_kpis PRIMARY KEY,
        organization_id  BIGINT        NOT NULL,
        user_id          BIGINT        NOT NULL,
        period           NVARCHAR(20)  NOT NULL,
        tasks_completed  INT           NOT NULL CONSTRAINT df_kpi_completed DEFAULT 0,
        tasks_on_time    INT           NOT NULL CONSTRAINT df_kpi_ontime DEFAULT 0,
        score            DECIMAL(5,2)  NOT NULL CONSTRAINT df_kpi_score DEFAULT 0,
        notes            NVARCHAR(MAX) NULL,
        created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_kpi_created DEFAULT SYSUTCDATETIME(),
        updated_at       DATETIME2(0)  NOT NULL CONSTRAINT df_kpi_updated DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_kpi_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT fk_kpi_user FOREIGN KEY (user_id) REFERENCES dbo.users(id)
    );
    CREATE INDEX ix_employee_kpis_org ON dbo.employee_kpis(organization_id);
END
GO

-- Upgrade legacy KPI columns (007_employee_kpis.sql shape)
IF OBJECT_ID('dbo.employee_kpis', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.employee_kpis', 'organization_id') IS NULL
        ALTER TABLE dbo.employee_kpis ADD organization_id BIGINT NULL;

    IF COL_LENGTH('dbo.employee_kpis', 'period') IS NULL AND COL_LENGTH('dbo.employee_kpis', 'evaluation_period') IS NOT NULL
        EXEC sp_rename 'dbo.employee_kpis.evaluation_period', 'period', 'COLUMN';

    IF COL_LENGTH('dbo.employee_kpis', 'period') IS NULL
        ALTER TABLE dbo.employee_kpis ADD period NVARCHAR(20) NULL;

    IF COL_LENGTH('dbo.employee_kpis', 'tasks_on_time') IS NULL
        ALTER TABLE dbo.employee_kpis ADD tasks_on_time INT NOT NULL CONSTRAINT df_kpi_ontime_upgrade DEFAULT 0;

    IF COL_LENGTH('dbo.employee_kpis', 'score') IS NULL AND COL_LENGTH('dbo.employee_kpis', 'performance_score') IS NOT NULL
        EXEC sp_rename 'dbo.employee_kpis.performance_score', 'score', 'COLUMN';

    IF COL_LENGTH('dbo.employee_kpis', 'score') IS NULL
        ALTER TABLE dbo.employee_kpis ADD score DECIMAL(5,2) NOT NULL CONSTRAINT df_kpi_score_upgrade DEFAULT 0;

    IF COL_LENGTH('dbo.employee_kpis', 'notes') IS NULL
        ALTER TABLE dbo.employee_kpis ADD notes NVARCHAR(MAX) NULL;

    IF COL_LENGTH('dbo.employee_kpis', 'created_at') IS NULL
        ALTER TABLE dbo.employee_kpis ADD created_at DATETIME2(0) NOT NULL CONSTRAINT df_kpi_created_upgrade DEFAULT SYSUTCDATETIME();

    IF COL_LENGTH('dbo.employee_kpis', 'updated_at') IS NULL
        ALTER TABLE dbo.employee_kpis ADD updated_at DATETIME2(0) NOT NULL CONSTRAINT df_kpi_updated_upgrade DEFAULT SYSUTCDATETIME();
END
GO

-- Backfill org + period, then harden NOT NULL
IF OBJECT_ID('dbo.employee_kpis', 'U') IS NOT NULL AND COL_LENGTH('dbo.employee_kpis', 'organization_id') IS NOT NULL
BEGIN
    DECLARE @org BIGINT = (SELECT TOP 1 id FROM dbo.organizations WHERE slug = N'default' ORDER BY id);
    IF @org IS NULL SET @org = (SELECT TOP 1 id FROM dbo.organizations ORDER BY id);

    IF @org IS NOT NULL
    BEGIN
        UPDATE k SET organization_id = COALESCE(k.organization_id, u.organization_id, @org)
        FROM dbo.employee_kpis k
        LEFT JOIN dbo.users u ON u.id = k.user_id
        WHERE k.organization_id IS NULL;

        UPDATE dbo.employee_kpis SET period = N'unknown' WHERE period IS NULL OR LTRIM(RTRIM(period)) = N'';

        ALTER TABLE dbo.employee_kpis ALTER COLUMN organization_id BIGINT NOT NULL;
        ALTER TABLE dbo.employee_kpis ALTER COLUMN period NVARCHAR(20) NOT NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_kpi_org')
        ALTER TABLE dbo.employee_kpis ADD CONSTRAINT fk_kpi_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_employee_kpis_org' AND object_id = OBJECT_ID(N'dbo.employee_kpis'))
        CREATE INDEX ix_employee_kpis_org ON dbo.employee_kpis(organization_id);
END
GO

-- Drop obsolete legacy KPI columns / FKs
IF OBJECT_ID('dbo.employee_kpis', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_kpi_project')
        ALTER TABLE dbo.employee_kpis DROP CONSTRAINT FK_kpi_project;
    IF COL_LENGTH('dbo.employee_kpis', 'project_id') IS NOT NULL
        ALTER TABLE dbo.employee_kpis DROP COLUMN project_id;
    IF COL_LENGTH('dbo.employee_kpis', 'delayed_tasks') IS NOT NULL
        ALTER TABLE dbo.employee_kpis DROP COLUMN delayed_tasks;
    IF COL_LENGTH('dbo.employee_kpis', 'on_time_rate') IS NOT NULL
        ALTER TABLE dbo.employee_kpis DROP COLUMN on_time_rate;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_kpi_on_time')
        ALTER TABLE dbo.employee_kpis DROP CONSTRAINT CK_kpi_on_time;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_kpi_score')
        ALTER TABLE dbo.employee_kpis DROP CONSTRAINT CK_kpi_score;
END
GO

-- ---------------------------------------------------------------------------
-- notifications
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.notifications (
        id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_notifications PRIMARY KEY,
        organization_id  BIGINT        NOT NULL,
        user_id          BIGINT        NOT NULL,
        title            NVARCHAR(200) NOT NULL,
        message          NVARCHAR(MAX) NULL,
        type             NVARCHAR(20)  NOT NULL CONSTRAINT df_notif_type DEFAULT N'INFO',
        is_read          BIT           NOT NULL CONSTRAINT df_notif_read DEFAULT 0,
        link             NVARCHAR(500) NULL,
        created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_notif_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_notif_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT fk_notif_user FOREIGN KEY (user_id) REFERENCES dbo.users(id)
    );
    CREATE INDEX ix_notifications_user ON dbo.notifications(user_id, is_read);
END
GO

IF OBJECT_ID('dbo.notifications', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.notifications', 'organization_id') IS NULL
        ALTER TABLE dbo.notifications ADD organization_id BIGINT NULL;
    IF COL_LENGTH('dbo.notifications', 'type') IS NULL
        ALTER TABLE dbo.notifications ADD type NVARCHAR(20) NOT NULL CONSTRAINT df_notif_type_upgrade DEFAULT N'INFO';
    IF COL_LENGTH('dbo.notifications', 'link') IS NULL
        ALTER TABLE dbo.notifications ADD link NVARCHAR(500) NULL;
END
GO

IF OBJECT_ID('dbo.notifications', 'U') IS NOT NULL AND COL_LENGTH('dbo.notifications', 'organization_id') IS NOT NULL
BEGIN
    DECLARE @orgN BIGINT = (SELECT TOP 1 id FROM dbo.organizations WHERE slug = N'default' ORDER BY id);
    IF @orgN IS NULL SET @orgN = (SELECT TOP 1 id FROM dbo.organizations ORDER BY id);

    IF @orgN IS NOT NULL
    BEGIN
        UPDATE n SET organization_id = COALESCE(n.organization_id, u.organization_id, @orgN)
        FROM dbo.notifications n
        LEFT JOIN dbo.users u ON u.id = n.user_id
        WHERE n.organization_id IS NULL;

        ALTER TABLE dbo.notifications ALTER COLUMN organization_id BIGINT NOT NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_notif_org')
        ALTER TABLE dbo.notifications ADD CONSTRAINT fk_notif_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_notifications_user' AND object_id = OBJECT_ID(N'dbo.notifications'))
        CREATE INDEX ix_notifications_user ON dbo.notifications(user_id, is_read);
END
GO

-- ---------------------------------------------------------------------------
-- project_files
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.project_files', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.project_files (
        id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_project_files PRIMARY KEY,
        organization_id  BIGINT         NOT NULL,
        project_id       BIGINT         NOT NULL,
        file_name        NVARCHAR(300)  NOT NULL,
        content_type     NVARCHAR(150)  NULL,
        size_bytes       BIGINT         NOT NULL CONSTRAINT df_pf_size DEFAULT 0,
        url              NVARCHAR(1000) NOT NULL,
        uploaded_by_id   BIGINT         NOT NULL,
        created_at       DATETIME2(0)   NOT NULL CONSTRAINT df_pf_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_pf_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT fk_pf_project FOREIGN KEY (project_id) REFERENCES dbo.projects(id),
        CONSTRAINT fk_pf_uploader FOREIGN KEY (uploaded_by_id) REFERENCES dbo.users(id)
    );
    CREATE INDEX ix_project_files_project ON dbo.project_files(project_id);
END
GO

IF OBJECT_ID('dbo.project_files', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('dbo.project_files', 'organization_id') IS NULL
        ALTER TABLE dbo.project_files ADD organization_id BIGINT NULL;

    IF COL_LENGTH('dbo.project_files', 'url') IS NULL AND COL_LENGTH('dbo.project_files', 'file_url') IS NOT NULL
        EXEC sp_rename 'dbo.project_files.file_url', 'url', 'COLUMN';

    IF COL_LENGTH('dbo.project_files', 'url') IS NULL
        ALTER TABLE dbo.project_files ADD url NVARCHAR(1000) NULL;

    IF COL_LENGTH('dbo.project_files', 'uploaded_by_id') IS NULL AND COL_LENGTH('dbo.project_files', 'uploaded_by') IS NOT NULL
        EXEC sp_rename 'dbo.project_files.uploaded_by', 'uploaded_by_id', 'COLUMN';

    IF COL_LENGTH('dbo.project_files', 'uploaded_by_id') IS NULL
        ALTER TABLE dbo.project_files ADD uploaded_by_id BIGINT NULL;

    IF COL_LENGTH('dbo.project_files', 'content_type') IS NULL
        ALTER TABLE dbo.project_files ADD content_type NVARCHAR(150) NULL;

    IF COL_LENGTH('dbo.project_files', 'size_bytes') IS NULL
        ALTER TABLE dbo.project_files ADD size_bytes BIGINT NOT NULL CONSTRAINT df_pf_size_upgrade DEFAULT 0;
END
GO

IF OBJECT_ID('dbo.project_files', 'U') IS NOT NULL AND COL_LENGTH('dbo.project_files', 'organization_id') IS NOT NULL
BEGIN
    DECLARE @orgF BIGINT = (SELECT TOP 1 id FROM dbo.organizations WHERE slug = N'default' ORDER BY id);
    IF @orgF IS NULL SET @orgF = (SELECT TOP 1 id FROM dbo.organizations ORDER BY id);

    IF @orgF IS NOT NULL
    BEGIN
        UPDATE f SET organization_id = COALESCE(f.organization_id, p.organization_id, @orgF)
        FROM dbo.project_files f
        LEFT JOIN dbo.projects p ON p.id = f.project_id
        WHERE f.organization_id IS NULL;

        UPDATE dbo.project_files SET url = N'' WHERE url IS NULL;

        IF EXISTS (SELECT 1 FROM dbo.project_files WHERE uploaded_by_id IS NULL)
        BEGIN
            DECLARE @anyUser BIGINT = (SELECT TOP 1 id FROM dbo.users ORDER BY id);
            IF @anyUser IS NOT NULL
                UPDATE dbo.project_files SET uploaded_by_id = @anyUser WHERE uploaded_by_id IS NULL;
        END

        ALTER TABLE dbo.project_files ALTER COLUMN organization_id BIGINT NOT NULL;
        ALTER TABLE dbo.project_files ALTER COLUMN url NVARCHAR(1000) NOT NULL;
        ALTER TABLE dbo.project_files ALTER COLUMN uploaded_by_id BIGINT NOT NULL;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_pf_org')
        ALTER TABLE dbo.project_files ADD CONSTRAINT fk_pf_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'fk_pf_uploader')
       AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_files_uploader')
        ALTER TABLE dbo.project_files ADD CONSTRAINT fk_pf_uploader FOREIGN KEY (uploaded_by_id) REFERENCES dbo.users(id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_project_files_project' AND object_id = OBJECT_ID(N'dbo.project_files'))
        CREATE INDEX ix_project_files_project ON dbo.project_files(project_id);
END
GO

PRINT '021_wave2_entities: create/upgrade completed.';
GO
