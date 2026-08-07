-- ============================================================================
-- 033 — Global unique email for active users (settled login policy).
-- Login / password-reset resolve by email alone; one active account per email.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.users', N'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_users_org_email_active' AND object_id = OBJECT_ID(N'dbo.users'))
        DROP INDEX UX_users_org_email_active ON dbo.users;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_users_email_active' AND object_id = OBJECT_ID(N'dbo.users'))
    BEGIN
        -- Fail loudly if duplicates would block the unique index.
        IF EXISTS (
            SELECT email
            FROM dbo.users
            WHERE is_deleted = 0
            GROUP BY email
            HAVING COUNT(*) > 1
        )
        BEGIN
            THROW 50001, '033_global_unique_email: duplicate active emails exist; resolve before applying.', 1;
        END;

        CREATE UNIQUE INDEX UX_users_email_active
            ON dbo.users (email)
            WHERE is_deleted = 0;
    END;
END;
GO

PRINT '033_global_unique_email: completed.';
GO
