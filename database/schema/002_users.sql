-- Stratix v1 — users (current implementation focus)
-- Requires: 001_departments.sql

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'users')
BEGIN
    CREATE TABLE users (
        id             BIGINT IDENTITY(1,1) NOT NULL,
        name           NVARCHAR(200)        NOT NULL,
        email          NVARCHAR(255)        NOT NULL,
        password       NVARCHAR(255)        NOT NULL,
        role           NVARCHAR(50)         NOT NULL,
        job_title      NVARCHAR(150)        NULL,
        status         NVARCHAR(30)         NOT NULL CONSTRAINT DF_users_status DEFAULT ('ACTIVE'),
        department_id  BIGINT               NULL,
        created_at     DATETIME2(0)         NOT NULL CONSTRAINT DF_users_created_at DEFAULT (SYSUTCDATETIME()),
        updated_at     DATETIME2(0)         NOT NULL CONSTRAINT DF_users_updated_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_users PRIMARY KEY (id),
        CONSTRAINT UQ_users_email UNIQUE (email),
        CONSTRAINT CK_users_role CHECK (role IN (
            'ADMIN', 'PROJECT_MANAGER', 'TEAM_LEADER', 'EMPLOYEE', 'EXECUTIVE_VIEWER'
        )),
        CONSTRAINT CK_users_status CHECK (status IN ('ACTIVE', 'INACTIVE', 'SUSPENDED')),
        CONSTRAINT FK_users_department FOREIGN KEY (department_id)
            REFERENCES departments (id) ON DELETE SET NULL
    );

    CREATE INDEX IX_users_department_id ON users (department_id);
    CREATE INDEX IX_users_role ON users (role);
    CREATE INDEX IX_users_status ON users (status);
END;
GO

-- Optional seed: default admin (change password before production)
-- Password below is BCrypt hash of "ChangeMe123!" — replace in deployment
/*
INSERT INTO departments (name, description) VALUES (N'IT', N'Information Technology');
INSERT INTO users (name, email, password, role, job_title, department_id)
VALUES (
    N'System Admin',
    N'admin@stratix.local',
    N'$2a$10$N9qo8uLOickgx2ZMRZoMyeIjZRGdjGj/n3.Y9uKqKqKqKqKqKqKqKq', -- placeholder
    N'ADMIN',
    N'Administrator',
    1
);
*/
