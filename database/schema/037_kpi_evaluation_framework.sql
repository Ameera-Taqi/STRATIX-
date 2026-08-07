-- ============================================================================
-- 037 — KPI evaluation framework (periods, definitions, evaluations, results, task quality).
-- Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.evaluation_periods', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.evaluation_periods
    (
        id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_evaluation_periods PRIMARY KEY,
        organization_id   BIGINT NOT NULL,
        name              NVARCHAR(150) NOT NULL,
        start_date        DATE NOT NULL,
        end_date          DATE NOT NULL,
        status            NVARCHAR(20) NOT NULL CONSTRAINT DF_eval_period_status DEFAULT (N'DRAFT'),
        created_at        DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_eval_period_created DEFAULT (SYSUTCDATETIME()),
        updated_at        DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_eval_period_updated DEFAULT (SYSUTCDATETIME()),
        is_deleted        BIT NOT NULL CONSTRAINT DF_eval_period_deleted DEFAULT (0),
        deleted_at        DATETIMEOFFSET(0) NULL,
        CONSTRAINT FK_eval_period_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT CK_eval_period_status CHECK (status IN (N'DRAFT', N'OPEN', N'CLOSED'))
    );
    CREATE INDEX IX_eval_periods_org ON dbo.evaluation_periods (organization_id) WHERE is_deleted = 0;
END
GO

IF OBJECT_ID(N'dbo.kpi_definitions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.kpi_definitions
    (
        id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_kpi_definitions PRIMARY KEY,
        organization_id   BIGINT NOT NULL,
        code              NVARCHAR(80) NOT NULL,
        name              NVARCHAR(150) NOT NULL,
        description       NVARCHAR(500) NULL,
        weight            DECIMAL(8,2) NOT NULL CONSTRAINT DF_kpi_def_weight DEFAULT (1),
        higher_is_better  BIT NOT NULL CONSTRAINT DF_kpi_def_hib DEFAULT (1),
        is_active         BIT NOT NULL CONSTRAINT DF_kpi_def_active DEFAULT (1),
        created_at        DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_kpi_def_created DEFAULT (SYSUTCDATETIME()),
        updated_at        DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_kpi_def_updated DEFAULT (SYSUTCDATETIME()),
        is_deleted        BIT NOT NULL CONSTRAINT DF_kpi_def_deleted DEFAULT (0),
        deleted_at        DATETIMEOFFSET(0) NULL,
        CONSTRAINT FK_kpi_def_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id)
    );
    CREATE UNIQUE INDEX UX_kpi_definitions_org_code
        ON dbo.kpi_definitions (organization_id, code) WHERE is_deleted = 0;
END
GO

IF OBJECT_ID(N'dbo.employee_evaluations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.employee_evaluations
    (
        id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_employee_evaluations PRIMARY KEY,
        organization_id   BIGINT NOT NULL,
        period_id         BIGINT NOT NULL,
        user_id           BIGINT NOT NULL,
        status            NVARCHAR(20) NOT NULL CONSTRAINT DF_emp_eval_status DEFAULT (N'DRAFT'),
        overall_score     DECIMAL(8,2) NULL,
        notes             NVARCHAR(2000) NULL,
        submitted_at      DATETIMEOFFSET(0) NULL,
        reviewed_by_id    BIGINT NULL,
        reviewed_at       DATETIMEOFFSET(0) NULL,
        approved_by_id    BIGINT NULL,
        approved_at       DATETIMEOFFSET(0) NULL,
        rejection_reason  NVARCHAR(1000) NULL,
        created_at        DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_emp_eval_created DEFAULT (SYSUTCDATETIME()),
        updated_at        DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_emp_eval_updated DEFAULT (SYSUTCDATETIME()),
        is_deleted        BIT NOT NULL CONSTRAINT DF_emp_eval_deleted DEFAULT (0),
        deleted_at        DATETIMEOFFSET(0) NULL,
        CONSTRAINT FK_emp_eval_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_emp_eval_period FOREIGN KEY (period_id) REFERENCES dbo.evaluation_periods(id),
        CONSTRAINT FK_emp_eval_user FOREIGN KEY (user_id) REFERENCES dbo.users(id),
        CONSTRAINT CK_emp_eval_status CHECK (status IN (N'DRAFT', N'SUBMITTED', N'IN_REVIEW', N'APPROVED', N'REJECTED'))
    );
    CREATE UNIQUE INDEX UX_emp_eval_period_user
        ON dbo.employee_evaluations (period_id, user_id) WHERE is_deleted = 0;
END
GO

IF OBJECT_ID(N'dbo.employee_kpi_results', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.employee_kpi_results
    (
        id                  BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_employee_kpi_results PRIMARY KEY,
        organization_id     BIGINT NOT NULL,
        evaluation_id       BIGINT NOT NULL,
        kpi_definition_id   BIGINT NOT NULL,
        calculated_value    DECIMAL(12,4) NOT NULL CONSTRAINT DF_kpi_res_calc DEFAULT (0),
        adjusted_value      DECIMAL(12,4) NULL,
        score               DECIMAL(8,2) NOT NULL CONSTRAINT DF_kpi_res_score DEFAULT (0),
        comment             NVARCHAR(1000) NULL,
        created_at          DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_kpi_res_created DEFAULT (SYSUTCDATETIME()),
        updated_at          DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_kpi_res_updated DEFAULT (SYSUTCDATETIME()),
        is_deleted          BIT NOT NULL CONSTRAINT DF_kpi_res_deleted DEFAULT (0),
        deleted_at          DATETIMEOFFSET(0) NULL,
        CONSTRAINT FK_kpi_res_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_kpi_res_eval FOREIGN KEY (evaluation_id) REFERENCES dbo.employee_evaluations(id),
        CONSTRAINT FK_kpi_res_def FOREIGN KEY (kpi_definition_id) REFERENCES dbo.kpi_definitions(id)
    );
    CREATE UNIQUE INDEX UX_kpi_res_eval_def
        ON dbo.employee_kpi_results (evaluation_id, kpi_definition_id) WHERE is_deleted = 0;
END
GO

IF OBJECT_ID(N'dbo.task_quality_evaluations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.task_quality_evaluations
    (
        id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_task_quality_evaluations PRIMARY KEY,
        organization_id   BIGINT NOT NULL,
        task_id           BIGINT NOT NULL,
        evaluator_id      BIGINT NOT NULL,
        quality_score     INT NOT NULL,
        notes             NVARCHAR(1000) NULL,
        created_at        DATETIMEOFFSET(0) NOT NULL CONSTRAINT DF_tqe_created DEFAULT (SYSUTCDATETIME()),
        is_deleted        BIT NOT NULL CONSTRAINT DF_tqe_deleted DEFAULT (0),
        deleted_at        DATETIMEOFFSET(0) NULL,
        CONSTRAINT FK_tqe_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT FK_tqe_task FOREIGN KEY (task_id) REFERENCES dbo.tasks(id),
        CONSTRAINT FK_tqe_evaluator FOREIGN KEY (evaluator_id) REFERENCES dbo.users(id),
        CONSTRAINT CK_tqe_score CHECK (quality_score BETWEEN 1 AND 5)
    );
    CREATE INDEX IX_tqe_task ON dbo.task_quality_evaluations (task_id) WHERE is_deleted = 0;
END
GO

PRINT '037_kpi_evaluation_framework: completed.';
GO
