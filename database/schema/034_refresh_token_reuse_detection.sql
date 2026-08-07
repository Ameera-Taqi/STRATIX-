-- ============================================================================
-- 034 — Refresh-token reuse detection (token family + rotation lineage). Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.refresh_tokens', N'token_family_id') IS NULL
    ALTER TABLE dbo.refresh_tokens ADD token_family_id UNIQUEIDENTIFIER NULL;
GO

IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.refresh_tokens', N'token_family_id') IS NOT NULL
BEGIN
    UPDATE dbo.refresh_tokens SET token_family_id = NEWID() WHERE token_family_id IS NULL;
    -- Make NOT NULL only when every row has a value.
    IF NOT EXISTS (SELECT 1 FROM dbo.refresh_tokens WHERE token_family_id IS NULL)
       AND EXISTS (
           SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID(N'dbo.refresh_tokens')
             AND name = N'token_family_id'
             AND is_nullable = 1)
        ALTER TABLE dbo.refresh_tokens ALTER COLUMN token_family_id UNIQUEIDENTIFIER NOT NULL;
END
GO

IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.refresh_tokens', N'replaced_by_token_hash') IS NULL
    ALTER TABLE dbo.refresh_tokens ADD replaced_by_token_hash NVARCHAR(64) NULL;
GO

IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL
   AND COL_LENGTH(N'dbo.refresh_tokens', N'reuse_detected_at') IS NULL
    ALTER TABLE dbo.refresh_tokens ADD reuse_detected_at DATETIME2(0) NULL;
GO

IF OBJECT_ID(N'dbo.refresh_tokens', N'U') IS NOT NULL
   AND NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = N'IX_refresh_tokens_family_user' AND object_id = OBJECT_ID(N'dbo.refresh_tokens'))
    CREATE INDEX IX_refresh_tokens_family_user ON dbo.refresh_tokens (token_family_id, user_id);
GO

PRINT '034_refresh_token_reuse_detection: completed.';
GO
