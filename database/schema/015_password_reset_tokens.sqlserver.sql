-- Stratix — password_reset_tokens table

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
