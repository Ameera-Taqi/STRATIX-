-- ============================================================================
-- 034 — Refresh-token reuse detection (token family + rotation lineage). Idempotent.
-- ============================================================================

IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL
BEGIN
    IF COL_LENGTH(N'dbo.refresh_tokens', N'token_family_id') IS NULL
    BEGIN
        ALTER TABLE dbo.refresh_tokens ADD token_family_id UNIQUEIDENTIFIER NULL;
        UPDATE dbo.refresh_tokens SET token_family_id = NEWID() WHERE token_family_id IS NULL;
        ALTER TABLE dbo.refresh_tokens ALTER COLUMN token_family_id UNIQUEIDENTIFIER NOT NULL;
    END

    IF COL_LENGTH(N'dbo.refresh_tokens', N'replaced_by_token_hash') IS NULL
        ALTER TABLE dbo.refresh_tokens ADD replaced_by_token_hash NVARCHAR(64) NULL;

    IF COL_LENGTH(N'dbo.refresh_tokens', N'reuse_detected_at') IS NULL
        ALTER TABLE dbo.refresh_tokens ADD reuse_detected_at DATETIME2(0) NULL;

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_refresh_tokens_family_user' AND object_id = OBJECT_ID(N'dbo.refresh_tokens'))
        CREATE INDEX IX_refresh_tokens_family_user ON dbo.refresh_tokens (token_family_id, user_id);
END
GO

PRINT '034_refresh_token_reuse_detection: completed.';
GO
