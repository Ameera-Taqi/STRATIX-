-- Stratix v1 — projects
-- Requires: 001_departments.sql, 002_users.sql

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'projects')
BEGIN
    CREATE TABLE projects (
        id                  BIGINT IDENTITY(1,1) NOT NULL,
        name                NVARCHAR(200)        NOT NULL,
        description         NVARCHAR(MAX)        NULL,
        status              NVARCHAR(30)         NOT NULL CONSTRAINT DF_projects_status DEFAULT ('PLANNED'),
        priority            NVARCHAR(20)         NOT NULL CONSTRAINT DF_projects_priority DEFAULT ('MEDIUM'),
        start_date          DATE                 NULL,
        end_date            DATE                 NULL,
        progress            DECIMAL(5,2)         NOT NULL CONSTRAINT DF_projects_progress DEFAULT (0),
        project_manager_id  BIGINT               NULL,
        department_id       BIGINT               NULL,
        created_at          DATETIME2(0)         NOT NULL CONSTRAINT DF_projects_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_projects PRIMARY KEY (id),
        CONSTRAINT CK_projects_status CHECK (status IN (
            'PLANNED', 'ACTIVE', 'ON_HOLD', 'COMPLETED', 'CANCELLED'
        )),
        CONSTRAINT CK_projects_priority CHECK (priority IN ('LOW', 'MEDIUM', 'HIGH', 'CRITICAL')),
        CONSTRAINT CK_projects_progress CHECK (progress >= 0 AND progress <= 100),
        CONSTRAINT FK_projects_manager FOREIGN KEY (project_manager_id)
            REFERENCES users (id) ON DELETE SET NULL,
        CONSTRAINT FK_projects_department FOREIGN KEY (department_id)
            REFERENCES departments (id) ON DELETE SET NULL
    );

    CREATE INDEX IX_projects_manager ON projects (project_manager_id);
    CREATE INDEX IX_projects_department ON projects (department_id);
    CREATE INDEX IX_projects_status ON projects (status);
END;
GO
