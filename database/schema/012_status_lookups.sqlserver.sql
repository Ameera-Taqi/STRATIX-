-- =============================================================================
-- Stratix — Status lookup tables (SQL Server)
-- Run after: 003_projects.sql, 005_tasks.sql
--
-- No application code changes required:
--   • Existing columns stay NVARCHAR (status)
--   • Values remain the same codes (PLANNED, TODO, OPEN, …)
--   • FK references project_statuses.code / task_statuses.code / risk_statuses.code
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Lookup tables
-- -----------------------------------------------------------------------------

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'project_statuses')
BEGIN
    CREATE TABLE project_statuses (
        id          SMALLINT      IDENTITY(1,1) NOT NULL,
        code        NVARCHAR(30)  NOT NULL,
        label_en    NVARCHAR(100) NOT NULL,
        label_ar    NVARCHAR(100) NOT NULL,
        sort_order  SMALLINT      NOT NULL CONSTRAINT DF_project_statuses_sort DEFAULT (0),
        is_active   BIT           NOT NULL CONSTRAINT DF_project_statuses_active DEFAULT (1),
        created_at  DATETIME2(0)  NOT NULL CONSTRAINT DF_project_statuses_created DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_project_statuses PRIMARY KEY (id),
        CONSTRAINT UQ_project_statuses_code UNIQUE (code)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'task_statuses')
BEGIN
    CREATE TABLE task_statuses (
        id          SMALLINT      IDENTITY(1,1) NOT NULL,
        code        NVARCHAR(30)  NOT NULL,
        label_en    NVARCHAR(100) NOT NULL,
        label_ar    NVARCHAR(100) NOT NULL,
        sort_order  SMALLINT      NOT NULL CONSTRAINT DF_task_statuses_sort DEFAULT (0),
        is_terminal BIT           NOT NULL CONSTRAINT DF_task_statuses_terminal DEFAULT (0),
        is_active   BIT           NOT NULL CONSTRAINT DF_task_statuses_active DEFAULT (1),
        created_at  DATETIME2(0)  NOT NULL CONSTRAINT DF_task_statuses_created DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_task_statuses PRIMARY KEY (id),
        CONSTRAINT UQ_task_statuses_code UNIQUE (code)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'risk_statuses')
BEGIN
    CREATE TABLE risk_statuses (
        id          SMALLINT      IDENTITY(1,1) NOT NULL,
        code        NVARCHAR(20)  NOT NULL,
        label_en    NVARCHAR(100) NOT NULL,
        label_ar    NVARCHAR(100) NOT NULL,
        sort_order  SMALLINT      NOT NULL CONSTRAINT DF_risk_statuses_sort DEFAULT (0),
        is_active   BIT           NOT NULL CONSTRAINT DF_risk_statuses_active DEFAULT (1),
        created_at  DATETIME2(0)  NOT NULL CONSTRAINT DF_risk_statuses_created DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_risk_statuses PRIMARY KEY (id),
        CONSTRAINT UQ_risk_statuses_code UNIQUE (code)
    );
END;
GO

-- -----------------------------------------------------------------------------
-- 2. Seed data
-- -----------------------------------------------------------------------------

MERGE project_statuses AS target
USING (VALUES
    (N'PLANNED',   N'Planned',   N'مخطط',        1),
    (N'ACTIVE',    N'Active',    N'نشط',         2),
    (N'ON_HOLD',   N'On Hold',   N'معلق',        3),
    (N'COMPLETED', N'Completed', N'مكتمل',       4),
    (N'CANCELLED', N'Cancelled', N'ملغى',        5)
) AS source (code, label_en, label_ar, sort_order)
ON target.code = source.code
WHEN NOT MATCHED THEN
    INSERT (code, label_en, label_ar, sort_order) VALUES (source.code, source.label_en, source.label_ar, source.sort_order);
GO

MERGE task_statuses AS target
USING (VALUES
    (N'TODO',        N'To Do',       N'قيد الانتظار', 1, 0),
    (N'IN_PROGRESS', N'In Progress', N'قيد التنفيذ',  2, 0),
    (N'REVIEW',      N'Review',      N'مراجعة',       3, 0),
    (N'DONE',        N'Done',        N'منجزة',        4, 1),
    (N'BLOCKED',     N'Blocked',     N'محجوبة',       5, 0)
) AS source (code, label_en, label_ar, sort_order, is_terminal)
ON target.code = source.code
WHEN NOT MATCHED THEN
    INSERT (code, label_en, label_ar, sort_order, is_terminal)
    VALUES (source.code, source.label_en, source.label_ar, source.sort_order, source.is_terminal);
GO

MERGE risk_statuses AS target
USING (VALUES
    (N'OPEN',       N'Open',       N'مفتوحة',       1),
    (N'MITIGATING', N'Mitigating', N'قيد المعالجة', 2),
    (N'CLOSED',     N'Closed',     N'مغلقة',        3)
) AS source (code, label_en, label_ar, sort_order)
ON target.code = source.code
WHEN NOT MATCHED THEN
    INSERT (code, label_en, label_ar, sort_order) VALUES (source.code, source.label_en, source.label_ar, source.sort_order);
GO

-- -----------------------------------------------------------------------------
-- 3. FK from entity tables → lookup.code (align lengths, drop CHECK first)
-- -----------------------------------------------------------------------------

-- Ensure lookup code columns are NVARCHAR(30) (matches CREATE above; older DBs may be 20)
IF OBJECT_ID(N'dbo.project_statuses', N'U') IS NOT NULL
    ALTER TABLE dbo.project_statuses ALTER COLUMN code NVARCHAR(30) NOT NULL;
IF OBJECT_ID(N'dbo.task_statuses', N'U') IS NOT NULL
    ALTER TABLE dbo.task_statuses ALTER COLUMN code NVARCHAR(30) NOT NULL;
IF OBJECT_ID(N'dbo.risk_statuses', N'U') IS NOT NULL
    ALTER TABLE dbo.risk_statuses ALTER COLUMN code NVARCHAR(30) NOT NULL;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'projects')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_projects_status')
        ALTER TABLE projects DROP CONSTRAINT CK_projects_status;

    ALTER TABLE projects ALTER COLUMN status NVARCHAR(30) NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_projects_status')
        ALTER TABLE projects
            ADD CONSTRAINT FK_projects_status
            FOREIGN KEY (status) REFERENCES project_statuses (code);
END;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tasks')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_tasks_status')
        ALTER TABLE tasks DROP CONSTRAINT CK_tasks_status;

    ALTER TABLE tasks ALTER COLUMN status NVARCHAR(30) NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_tasks_status')
        ALTER TABLE tasks
            ADD CONSTRAINT FK_tasks_status
            FOREIGN KEY (status) REFERENCES task_statuses (code);
END;
GO

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'project_risks')
BEGIN
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'ck_project_risks_status')
        ALTER TABLE project_risks DROP CONSTRAINT ck_project_risks_status;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_project_risks_status')
        ALTER TABLE project_risks DROP CONSTRAINT CK_project_risks_status;

    ALTER TABLE project_risks ALTER COLUMN status NVARCHAR(30) NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_project_risks_status')
        ALTER TABLE project_risks
            ADD CONSTRAINT FK_project_risks_status
            FOREIGN KEY (status) REFERENCES risk_statuses (code);
END;
GO
