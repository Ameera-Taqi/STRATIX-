-- Stratix v1 — project_files

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'project_files')
BEGIN
    CREATE TABLE project_files (
        id          BIGINT IDENTITY(1,1) NOT NULL,
        project_id  BIGINT               NOT NULL,
        uploaded_by BIGINT               NOT NULL,
        file_name   NVARCHAR(500)        NOT NULL,
        file_url    NVARCHAR(1000)       NOT NULL,
        created_at  DATETIME2(0)         NOT NULL CONSTRAINT DF_project_files_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_project_files PRIMARY KEY (id),
        CONSTRAINT FK_files_project FOREIGN KEY (project_id)
            REFERENCES projects (id) ON DELETE CASCADE,
        CONSTRAINT FK_files_uploader FOREIGN KEY (uploaded_by)
            REFERENCES users (id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_project_files_project ON project_files (project_id);
END;
GO
