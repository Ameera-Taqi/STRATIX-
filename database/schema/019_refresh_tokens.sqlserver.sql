-- ============================================================================
-- 019 — Refresh tokens (rotating, revocable sessions). Idempotent.
-- ============================================================================
IF OBJECT_ID('dbo.refresh_tokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.refresh_tokens (
        id                     BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_refresh_tokens PRIMARY KEY,
        organization_id        BIGINT           NOT NULL,
        user_id                BIGINT           NOT NULL,
        token_hash             NVARCHAR(64)     NOT NULL,
        token_family_id        UNIQUEIDENTIFIER NOT NULL CONSTRAINT df_refresh_tokens_family DEFAULT NEWID(),
        replaced_by_token_hash NVARCHAR(64)     NULL,
        reuse_detected_at      DATETIME2(0)     NULL,
        expires_at             DATETIME2(0)     NOT NULL,
        revoked_at             DATETIME2(0)     NULL,
        created_at             DATETIME2(0)     NOT NULL CONSTRAINT df_refresh_tokens_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_refresh_tokens_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT fk_refresh_tokens_user FOREIGN KEY (user_id) REFERENCES dbo.users(id)
    );
    CREATE INDEX ix_refresh_tokens_token_hash ON dbo.refresh_tokens(token_hash);
    CREATE INDEX ix_refresh_tokens_organization_id ON dbo.refresh_tokens(organization_id);
    CREATE INDEX IX_refresh_tokens_family_user ON dbo.refresh_tokens(token_family_id, user_id);
END
GO

PRINT '019_refresh_tokens: completed.';
GO
