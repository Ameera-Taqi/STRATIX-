-- ============================================================================
-- 022 — task_comments: tenant scoping + multi-tenancy alignment. Idempotent.
-- Adds organization_id (backfilled from the parent task) so comments are
-- filtered per-tenant like every other entity.
-- ============================================================================

-- 1) Create the table if it doesn't exist yet (fresh installs).
IF OBJECT_ID('dbo.task_comments', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.task_comments (
        id              BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_task_comments PRIMARY KEY,
        organization_id BIGINT        NOT NULL,
        task_id         BIGINT        NOT NULL,
        user_id         BIGINT        NOT NULL,
        comment         NVARCHAR(MAX) NOT NULL,
        created_at      DATETIME2(0)  NOT NULL CONSTRAINT df_task_comments_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_task_comments_org  FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id),
        CONSTRAINT fk_task_comments_task FOREIGN KEY (task_id) REFERENCES dbo.tasks(id) ON DELETE CASCADE,
        CONSTRAINT fk_task_comments_user FOREIGN KEY (user_id) REFERENCES dbo.users(id)
    );
    CREATE INDEX ix_task_comments_task ON dbo.task_comments(task_id);
    CREATE INDEX ix_task_comments_org ON dbo.task_comments(organization_id);
END
GO

-- 2) Existing table (pre-multitenancy) — add the column, backfill, enforce NOT NULL + FK.
IF OBJECT_ID('dbo.task_comments', 'U') IS NOT NULL AND COL_LENGTH('dbo.task_comments', 'organization_id') IS NULL
BEGIN
    ALTER TABLE dbo.task_comments ADD organization_id BIGINT NULL;
END
GO

IF OBJECT_ID('dbo.task_comments', 'U') IS NOT NULL AND COL_LENGTH('dbo.task_comments', 'organization_id') IS NOT NULL
BEGIN
    UPDATE tc
    SET tc.organization_id = t.organization_id
    FROM dbo.task_comments tc
    JOIN dbo.tasks t ON t.id = tc.task_id
    WHERE tc.organization_id IS NULL;

    -- Any orphaned rows (task missing) fall back to the default tenant.
    UPDATE dbo.task_comments
    SET organization_id = (SELECT id FROM dbo.organizations WHERE slug = 'default')
    WHERE organization_id IS NULL;

    ALTER TABLE dbo.task_comments ALTER COLUMN organization_id BIGINT NOT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_task_comments_org')
        ALTER TABLE dbo.task_comments ADD CONSTRAINT fk_task_comments_org FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_task_comments_org')
        CREATE INDEX ix_task_comments_org ON dbo.task_comments(organization_id);
END
GO

PRINT '022_task_comments_org: completed.';
GO
