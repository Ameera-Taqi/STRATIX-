-- ============================================================================
-- 036 — Progress effort fields, task transition reasons, health snapshots.
-- Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.tasks', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.tasks', N'estimated_hours') IS NULL
        ALTER TABLE dbo.tasks ADD estimated_hours DECIMAL(10,2) NOT NULL CONSTRAINT DF_tasks_estimated_hours DEFAULT (1);

    IF COL_LENGTH(N'dbo.tasks', N'actual_hours') IS NULL
        ALTER TABLE dbo.tasks ADD actual_hours DECIMAL(10,2) NULL;

    IF COL_LENGTH(N'dbo.tasks', N'blocked_reason') IS NULL
        ALTER TABLE dbo.tasks ADD blocked_reason NVARCHAR(1000) NULL;

    IF COL_LENGTH(N'dbo.tasks', N'reopen_reason') IS NULL
        ALTER TABLE dbo.tasks ADD reopen_reason NVARCHAR(1000) NULL;

    IF COL_LENGTH(N'dbo.tasks', N'review_reason') IS NULL
        ALTER TABLE dbo.tasks ADD review_reason NVARCHAR(1000) NULL;
END
GO

IF OBJECT_ID(N'dbo.project_health_snapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.project_health_snapshots
    (
        id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_project_health_snapshots PRIMARY KEY,
        organization_id   BIGINT NOT NULL,
        project_id        BIGINT NOT NULL,
        score             DECIMAL(5,2) NOT NULL,
        status            NVARCHAR(20) NOT NULL,
        progress          DECIMAL(5,2) NOT NULL,
        on_time_tasks     INT NOT NULL CONSTRAINT DF_phs_on_time DEFAULT (0),
        delayed_tasks     INT NOT NULL CONSTRAINT DF_phs_delayed DEFAULT (0),
        critical_risks    INT NOT NULL CONSTRAINT DF_phs_critical DEFAULT (0),
        note_key          NVARCHAR(80) NULL,
        captured_at       DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_phs_captured DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_phs_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_phs_project FOREIGN KEY (project_id) REFERENCES dbo.projects(id)
    );

    CREATE INDEX IX_phs_project_captured ON dbo.project_health_snapshots (project_id, captured_at DESC);
    CREATE INDEX IX_phs_org_captured ON dbo.project_health_snapshots (organization_id, captured_at DESC);
END
GO

PRINT '036_progress_health_execution: completed.';
GO
