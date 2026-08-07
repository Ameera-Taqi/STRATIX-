-- LEGACY v1 shape — do not include in 000_run_all.sql. Superseded by 021_wave2_entities.sqlserver.sql (create + upgrade).
-- Stratix v1 — notifications

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'notifications')
BEGIN
    CREATE TABLE notifications (
        id         BIGINT IDENTITY(1,1) NOT NULL,
        user_id    BIGINT               NOT NULL,
        title      NVARCHAR(200)        NOT NULL,
        message    NVARCHAR(MAX)        NOT NULL,
        is_read    BIT                  NOT NULL CONSTRAINT DF_notifications_is_read DEFAULT (0),
        created_at DATETIME2(0)         NOT NULL CONSTRAINT DF_notifications_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_notifications PRIMARY KEY (id),
        CONSTRAINT FK_notifications_user FOREIGN KEY (user_id)
            REFERENCES users (id) ON DELETE CASCADE
    );

    CREATE INDEX IX_notifications_user ON notifications (user_id);
    CREATE INDEX IX_notifications_unread ON notifications (user_id, is_read) WHERE is_read = 0;
END;
GO
