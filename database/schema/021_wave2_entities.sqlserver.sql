-- ============================================================================
-- 021 — Wave 2 tenant-scoped entities: change_requests, employee_kpis,
--       notifications, project_files. Idempotent.
-- ============================================================================
IF OBJECT_ID('dbo.change_requests', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.change_requests (
        id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_change_requests PRIMARY KEY,
        organization_id  BIGINT        NOT NULL,
        project_id       BIGINT        NOT NULL,
        title            NVARCHAR(200) NOT NULL,
        description      NVARCHAR(MAX) NULL,
        status           NVARCHAR(20)  NOT NULL CONSTRAINT df_cr_status DEFAULT 'PENDING',
        priority         NVARCHAR(20)  NOT NULL CONSTRAINT df_cr_priority DEFAULT 'MEDIUM',
        requested_by_id  BIGINT        NOT NULL,
        reviewed_by_id   BIGINT        NULL,
        created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_cr_created DEFAULT SYSUTCDATETIME(),
        updated_at       DATETIME2(0)  NOT NULL CONSTRAINT df_cr_updated DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_cr_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT fk_cr_project FOREIGN KEY (project_id) REFERENCES dbo.projects(id),
        CONSTRAINT fk_cr_requested FOREIGN KEY (requested_by_id) REFERENCES dbo.users(id),
        CONSTRAINT fk_cr_reviewed FOREIGN KEY (reviewed_by_id) REFERENCES dbo.users(id)
    );
    CREATE INDEX ix_change_requests_org ON dbo.change_requests(organization_id);
    CREATE INDEX ix_change_requests_project ON dbo.change_requests(project_id);
END
GO

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

IF OBJECT_ID('dbo.notifications', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.notifications (
        id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_notifications PRIMARY KEY,
        organization_id  BIGINT        NOT NULL,
        user_id          BIGINT        NOT NULL,
        title            NVARCHAR(200) NOT NULL,
        message          NVARCHAR(MAX) NULL,
        type             NVARCHAR(20)  NOT NULL CONSTRAINT df_notif_type DEFAULT 'INFO',
        is_read          BIT           NOT NULL CONSTRAINT df_notif_read DEFAULT 0,
        link             NVARCHAR(500) NULL,
        created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_notif_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_notif_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT fk_notif_user FOREIGN KEY (user_id) REFERENCES dbo.users(id)
    );
    CREATE INDEX ix_notifications_user ON dbo.notifications(user_id, is_read);
END
GO

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

PRINT '021_wave2_entities: completed.';
GO
