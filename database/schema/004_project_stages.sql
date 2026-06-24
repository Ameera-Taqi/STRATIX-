-- Stratix v1 — project_stages

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'project_stages')
BEGIN
    CREATE TABLE project_stages (
        id          BIGINT IDENTITY(1,1) NOT NULL,
        project_id  BIGINT               NOT NULL,
        name        NVARCHAR(200)        NOT NULL,
        description NVARCHAR(MAX)        NULL,
        status      NVARCHAR(30)         NOT NULL CONSTRAINT DF_stages_status DEFAULT ('TODO'),
        start_date  DATE                 NULL,
        end_date    DATE                 NULL,
        progress    DECIMAL(5,2)         NOT NULL CONSTRAINT DF_stages_progress DEFAULT (0),
        order_number INT                 NOT NULL CONSTRAINT DF_stages_order DEFAULT (1),

        CONSTRAINT PK_project_stages PRIMARY KEY (id),
        CONSTRAINT CK_stages_progress CHECK (progress >= 0 AND progress <= 100),
        CONSTRAINT FK_stages_project FOREIGN KEY (project_id)
            REFERENCES projects (id) ON DELETE CASCADE
    );

    CREATE INDEX IX_stages_project ON project_stages (project_id);
    CREATE UNIQUE INDEX UQ_stages_project_order ON project_stages (project_id, order_number);
END;
GO
