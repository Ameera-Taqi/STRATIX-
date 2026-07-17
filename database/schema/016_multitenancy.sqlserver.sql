-- ============================================================================
-- 016 — Multi-tenancy (SaaS): shared database + shared schema
-- Adds the organizations table and an organization_id foreign key to every
-- tenant-scoped table, backfilling existing rows into a single default tenant.
-- Idempotent: safe to run repeatedly.
-- ============================================================================

-- 1) organizations table --------------------------------------------------
IF OBJECT_ID('dbo.organizations', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.organizations (
        id                BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_organizations PRIMARY KEY,
        name              NVARCHAR(200)  NOT NULL,
        slug              NVARCHAR(100)  NOT NULL,
        status            NVARCHAR(20)   NOT NULL CONSTRAINT df_organizations_status DEFAULT 'ACTIVE',
        subscription_plan NVARCHAR(20)   NOT NULL CONSTRAINT df_organizations_plan   DEFAULT 'FREE',
        created_at        DATETIME2(0)   NOT NULL CONSTRAINT df_organizations_created DEFAULT SYSUTCDATETIME(),
        updated_at        DATETIME2(0)   NOT NULL CONSTRAINT df_organizations_updated DEFAULT SYSUTCDATETIME()
    );
    CREATE UNIQUE INDEX ux_organizations_slug ON dbo.organizations(slug);
END
GO

-- 2) default tenant (backfill target) -------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.organizations WHERE slug = 'default')
    INSERT INTO dbo.organizations (name, slug, status, subscription_plan)
    VALUES (N'Stratix', 'default', 'ACTIVE', 'ENTERPRISE');
GO

-- 3) add organization_id to every tenant-scoped table ---------------------
--    Pattern per table: add nullable column -> backfill -> NOT NULL -> FK.
DECLARE @tables TABLE (name SYSNAME);
INSERT INTO @tables (name) VALUES
    ('users'), ('departments'), ('projects'), ('project_stages'),
    ('tasks'), ('project_risks'), ('audit_logs'), ('project_milestones');

DECLARE @t SYSNAME, @sql NVARCHAR(MAX);
DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT name FROM @tables;
OPEN cur;
FETCH NEXT FROM cur INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID('dbo.' + @t, 'U') IS NOT NULL AND COL_LENGTH('dbo.' + @t, 'organization_id') IS NULL
    BEGIN
        SET @sql = 'ALTER TABLE dbo.' + QUOTENAME(@t) + ' ADD organization_id BIGINT NULL;';
        EXEC sp_executesql @sql;
    END
    FETCH NEXT FROM cur INTO @t;
END
CLOSE cur; DEALLOCATE cur;
GO

-- 4) backfill + enforce NOT NULL + FK (one committed batch per step) -------
DECLARE @defaultOrg BIGINT = (SELECT id FROM dbo.organizations WHERE slug = 'default');

DECLARE @t2 SYSNAME, @sql2 NVARCHAR(MAX);
DECLARE cur2 CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM (VALUES
        ('users'), ('departments'), ('projects'), ('project_stages'),
        ('tasks'), ('project_risks'), ('audit_logs'), ('project_milestones')) v(name)
    WHERE COL_LENGTH('dbo.' + name, 'organization_id') IS NOT NULL;
OPEN cur2;
FETCH NEXT FROM cur2 INTO @t2;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql2 = 'UPDATE dbo.' + QUOTENAME(@t2) + ' SET organization_id = ' + CAST(@defaultOrg AS NVARCHAR(20)) + ' WHERE organization_id IS NULL;';
    EXEC sp_executesql @sql2;

    SET @sql2 = 'ALTER TABLE dbo.' + QUOTENAME(@t2) + ' ALTER COLUMN organization_id BIGINT NOT NULL;';
    EXEC sp_executesql @sql2;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'fk_' + @t2 + '_organization')
    BEGIN
        SET @sql2 = 'ALTER TABLE dbo.' + QUOTENAME(@t2) + ' ADD CONSTRAINT fk_' + @t2 + '_organization ' +
                    'FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id);';
        EXEC sp_executesql @sql2;
    END

    -- Helpful covering index for the always-present tenant filter.
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'ix_' + @t2 + '_organization_id')
    BEGIN
        SET @sql2 = 'CREATE INDEX ix_' + @t2 + '_organization_id ON dbo.' + QUOTENAME(@t2) + '(organization_id);';
        EXEC sp_executesql @sql2;
    END

    FETCH NEXT FROM cur2 INTO @t2;
END
CLOSE cur2; DEALLOCATE cur2;
GO

PRINT '016_multitenancy: completed.';
GO
