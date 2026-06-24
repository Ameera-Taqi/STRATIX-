-- Stratix — optional baseline schema (SQL Server)
-- Hibernate ddl-auto=update is the primary schema manager in Docker.
-- Use this script for validate-only deployments or manual SSMS setup.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'departments')
BEGIN
    CREATE TABLE departments (
        id          BIGINT IDENTITY(1,1) NOT NULL,
        name        NVARCHAR(150)        NOT NULL,
        description NVARCHAR(500)        NULL,
        created_at  DATETIME2(0)         NOT NULL CONSTRAINT DF_departments_created_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_departments PRIMARY KEY (id),
        CONSTRAINT UQ_departments_name UNIQUE (name)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'users')
BEGIN
    CREATE TABLE users (
        id             BIGINT IDENTITY(1,1) NOT NULL,
        name           NVARCHAR(200)        NOT NULL,
        email          NVARCHAR(255)        NOT NULL,
        password       NVARCHAR(255)        NOT NULL,
        role           NVARCHAR(50)         NOT NULL,
        job_title      NVARCHAR(150)        NULL,
        status         NVARCHAR(30)         NOT NULL CONSTRAINT DF_users_status DEFAULT (N'ACTIVE'),
        department_id  BIGINT               NULL,
        created_at     DATETIME2(0)         NOT NULL CONSTRAINT DF_users_created_at DEFAULT (SYSUTCDATETIME()),
        updated_at     DATETIME2(0)         NOT NULL CONSTRAINT DF_users_updated_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_users PRIMARY KEY (id),
        CONSTRAINT UQ_users_email UNIQUE (email),
        CONSTRAINT FK_users_department FOREIGN KEY (department_id) REFERENCES departments (id) ON DELETE SET NULL
    );
    CREATE INDEX IX_users_department_id ON users (department_id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'projects')
BEGIN
    CREATE TABLE projects (
        id                  BIGINT IDENTITY(1,1) NOT NULL,
        name                NVARCHAR(200)        NOT NULL,
        description         NVARCHAR(MAX)        NULL,
        status              NVARCHAR(30)         NOT NULL CONSTRAINT DF_projects_status DEFAULT (N'PLANNED'),
        priority            NVARCHAR(20)         NOT NULL CONSTRAINT DF_projects_priority DEFAULT (N'MEDIUM'),
        start_date          DATE                 NULL,
        end_date            DATE                 NULL,
        progress            DECIMAL(5,2)         NOT NULL CONSTRAINT DF_projects_progress DEFAULT (0),
        project_manager_id  BIGINT               NULL,
        department_id       BIGINT               NULL,
        created_at          DATETIME2(0)         NOT NULL CONSTRAINT DF_projects_created_at DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_projects PRIMARY KEY (id),
        CONSTRAINT FK_projects_manager FOREIGN KEY (project_manager_id) REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT FK_projects_department FOREIGN KEY (department_id) REFERENCES departments (id) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'project_stages')
BEGIN
    CREATE TABLE project_stages (
        id           BIGINT IDENTITY(1,1) NOT NULL,
        project_id   BIGINT               NOT NULL,
        name         NVARCHAR(200)        NOT NULL,
        description  NVARCHAR(MAX)        NULL,
        status       NVARCHAR(20)         NOT NULL CONSTRAINT DF_stages_status DEFAULT (N'PLANNED'),
        start_date   DATE                 NULL,
        end_date     DATE                 NULL,
        progress     DECIMAL(5,2)         NOT NULL CONSTRAINT DF_stages_progress DEFAULT (0),
        order_number INT                  NOT NULL CONSTRAINT DF_stages_order DEFAULT (1),
        created_at   DATETIME2(0)         NOT NULL CONSTRAINT DF_stages_created DEFAULT (SYSUTCDATETIME()),
        updated_at   DATETIME2(0)         NOT NULL CONSTRAINT DF_stages_updated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_project_stages PRIMARY KEY (id),
        CONSTRAINT FK_stages_project FOREIGN KEY (project_id) REFERENCES projects (id) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'tasks')
BEGIN
    CREATE TABLE tasks (
        id           BIGINT IDENTITY(1,1) NOT NULL,
        project_id   BIGINT               NOT NULL,
        stage_id     BIGINT               NULL,
        title        NVARCHAR(200)        NOT NULL,
        description  NVARCHAR(MAX)        NULL,
        status       NVARCHAR(20)         NOT NULL CONSTRAINT DF_tasks_status DEFAULT (N'TODO'),
        priority     NVARCHAR(20)         NOT NULL CONSTRAINT DF_tasks_priority DEFAULT (N'MEDIUM'),
        assignee_id  BIGINT               NULL,
        start_date   DATE                 NULL,
        due_date     DATE                 NULL,
        completed_at DATETIME2(0)         NULL,
        progress     DECIMAL(5,2)         NOT NULL CONSTRAINT DF_tasks_progress DEFAULT (0),
        created_at   DATETIME2(0)         NOT NULL CONSTRAINT DF_tasks_created DEFAULT (SYSUTCDATETIME()),
        updated_at   DATETIME2(0)         NOT NULL CONSTRAINT DF_tasks_updated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_tasks PRIMARY KEY (id),
        CONSTRAINT FK_tasks_project FOREIGN KEY (project_id) REFERENCES projects (id) ON DELETE CASCADE,
        CONSTRAINT FK_tasks_stage FOREIGN KEY (stage_id) REFERENCES project_stages (id),
        CONSTRAINT FK_tasks_assignee FOREIGN KEY (assignee_id) REFERENCES users (id) ON DELETE SET NULL
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'project_risks')
BEGIN
    CREATE TABLE project_risks (
        id               BIGINT IDENTITY(1,1) NOT NULL,
        title            NVARCHAR(200)        NOT NULL,
        description      NVARCHAR(MAX)        NULL,
        impact           NVARCHAR(20)         NOT NULL,
        probability      NVARCHAR(20)         NOT NULL,
        risk_level       NVARCHAR(20)         NOT NULL,
        mitigation_plan  NVARCHAR(MAX)        NULL,
        status           NVARCHAR(20)         NOT NULL CONSTRAINT DF_project_risks_status DEFAULT (N'OPEN'),
        project_id       BIGINT               NOT NULL,
        owner_id         BIGINT               NOT NULL,
        created_at       DATETIME2(0)         NOT NULL CONSTRAINT DF_project_risks_created DEFAULT (SYSUTCDATETIME()),
        updated_at       DATETIME2(0)         NOT NULL CONSTRAINT DF_project_risks_updated DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_project_risks PRIMARY KEY (id),
        CONSTRAINT FK_project_risks_project FOREIGN KEY (project_id) REFERENCES projects (id) ON DELETE CASCADE,
        CONSTRAINT FK_project_risks_owner FOREIGN KEY (owner_id) REFERENCES users (id)
    );
END;
GO

PRINT N'Stratix baseline schema applied (entity-aligned).';
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'audit_logs')
BEGIN
    CREATE TABLE audit_logs (
        id           BIGINT IDENTITY(1,1) NOT NULL,
        user_id      BIGINT               NULL,
        user_name    NVARCHAR(200)        NULL,
        action       NVARCHAR(30)         NOT NULL,
        entity_type  NVARCHAR(30)         NOT NULL,
        entity_id    BIGINT               NOT NULL,
        entity_name  NVARCHAR(300)        NOT NULL,
        old_values   NVARCHAR(MAX)        NULL,
        new_values   NVARCHAR(MAX)        NULL,
        description  NVARCHAR(1000)       NOT NULL,
        ip_address   NVARCHAR(45)         NULL,
        user_agent   NVARCHAR(500)        NULL,
        project_id   BIGINT               NULL,
        project_name NVARCHAR(200)        NULL,
        created_at   DATETIME2(0)         NOT NULL CONSTRAINT DF_audit_logs_created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_audit_logs PRIMARY KEY (id)
    );
    CREATE INDEX IX_audit_logs_created_at ON audit_logs (created_at DESC);
    CREATE INDEX IX_audit_logs_entity ON audit_logs (entity_type, entity_id);
    CREATE INDEX IX_audit_logs_user_id ON audit_logs (user_id);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'password_reset_tokens')
BEGIN
    CREATE TABLE password_reset_tokens (
        id          BIGINT IDENTITY(1,1) NOT NULL,
        user_id     BIGINT               NOT NULL,
        token_hash  NVARCHAR(64)         NOT NULL,
        expires_at  DATETIME2(0)         NOT NULL,
        used_at     DATETIME2(0)         NULL,
        request_ip  NVARCHAR(45)         NULL,
        created_at  DATETIME2(0)         NOT NULL CONSTRAINT DF_password_reset_tokens_created DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_password_reset_tokens PRIMARY KEY (id),
        CONSTRAINT FK_password_reset_tokens_user FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    CREATE INDEX IX_password_reset_tokens_user ON password_reset_tokens (user_id);
    CREATE INDEX IX_password_reset_tokens_hash ON password_reset_tokens (token_hash);
END;
GO
