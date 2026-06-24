-- Stratix v1 — task_comments

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'task_comments')
BEGIN
    CREATE TABLE task_comments (
        id         BIGINT IDENTITY(1,1) NOT NULL,
        task_id    BIGINT               NOT NULL,
        user_id    BIGINT               NOT NULL,
        comment    NVARCHAR(MAX)        NOT NULL,
        created_at DATETIME2(0)         NOT NULL CONSTRAINT DF_task_comments_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_task_comments PRIMARY KEY (id),
        CONSTRAINT FK_comments_task FOREIGN KEY (task_id)
            REFERENCES tasks (id) ON DELETE CASCADE,
        CONSTRAINT FK_comments_user FOREIGN KEY (user_id)
            REFERENCES users (id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_task_comments_task ON task_comments (task_id);
END;
GO
