-- ============================================================================
-- 020 — Brute-force protection: per-account failed-attempt tracking + lockout.
-- Idempotent.
-- ============================================================================
IF COL_LENGTH('dbo.users', 'failed_login_attempts') IS NULL
    ALTER TABLE dbo.users ADD failed_login_attempts INT NOT NULL CONSTRAINT df_users_failed_logins DEFAULT 0;
GO

IF COL_LENGTH('dbo.users', 'lockout_until') IS NULL
    ALTER TABLE dbo.users ADD lockout_until DATETIME2(0) NULL;
GO

PRINT '020_login_lockout: completed.';
GO
