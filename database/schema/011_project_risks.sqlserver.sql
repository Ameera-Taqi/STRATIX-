-- Stratix — project risks (SQL Server)
-- Requires: users, projects
-- organization_id → 016_multitenancy.sqlserver.sql · soft-delete → 028_soft_delete.sql

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
        CONSTRAINT FK_project_risks_project FOREIGN KEY (project_id)
            REFERENCES projects (id) ON DELETE CASCADE,
        CONSTRAINT FK_project_risks_owner FOREIGN KEY (owner_id)
            REFERENCES users (id)
    );

    CREATE INDEX IX_project_risks_project ON project_risks (project_id);
    CREATE INDEX IX_project_risks_owner ON project_risks (owner_id);
    CREATE INDEX IX_project_risks_status ON project_risks (status);
    CREATE INDEX IX_project_risks_level ON project_risks (risk_level);
END;
GO
