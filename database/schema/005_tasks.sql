-- Stratix v1 — tasks

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'tasks')
BEGIN
    CREATE TABLE tasks (
        id           BIGINT IDENTITY(1,1) NOT NULL,
        project_id   BIGINT               NOT NULL,
        stage_id     BIGINT               NULL,
        title        NVARCHAR(300)        NOT NULL,
        description  NVARCHAR(MAX)        NULL,
        status       NVARCHAR(30)         NOT NULL CONSTRAINT DF_tasks_status DEFAULT ('TODO'),
        priority     NVARCHAR(20)         NOT NULL CONSTRAINT DF_tasks_priority DEFAULT ('MEDIUM'),
        assigned_to  BIGINT               NULL,
        start_date   DATE                 NULL,
        due_date     DATE                 NULL,
        completed_at DATETIME2(0)         NULL,
        progress     DECIMAL(5,2)         NOT NULL CONSTRAINT DF_tasks_progress DEFAULT (0),

        CONSTRAINT PK_tasks PRIMARY KEY (id),
        CONSTRAINT CK_tasks_status CHECK (status IN (
            'TODO', 'IN_PROGRESS', 'REVIEW', 'DONE', 'BLOCKED'
        )),
        CONSTRAINT CK_tasks_priority CHECK (priority IN ('LOW', 'MEDIUM', 'HIGH', 'URGENT')),
        CONSTRAINT CK_tasks_progress CHECK (progress >= 0 AND progress <= 100),
        CONSTRAINT FK_tasks_project FOREIGN KEY (project_id)
            REFERENCES projects (id) ON DELETE CASCADE,
        CONSTRAINT FK_tasks_stage FOREIGN KEY (stage_id)
            REFERENCES project_stages (id) ON DELETE NO ACTION,
        CONSTRAINT FK_tasks_assignee FOREIGN KEY (assigned_to)
            REFERENCES users (id) ON DELETE SET NULL
    );

    CREATE INDEX IX_tasks_project ON tasks (project_id);
    CREATE INDEX IX_tasks_stage ON tasks (stage_id);
    CREATE INDEX IX_tasks_assignee ON tasks (assigned_to);
    CREATE INDEX IX_tasks_status ON tasks (status);
END;
GO
