-- ============================================================================
-- 046 — Audit trail table used after create/update/delete of tenant entities.
-- Missing from earlier schema (016/032 assumed it already existed). Idempotent.
-- ============================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

IF OBJECT_ID(N'dbo.audit_logs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.audit_logs (
        id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT pk_audit_logs PRIMARY KEY,
        organization_id  BIGINT        NOT NULL,
        user_id          BIGINT        NULL,
        user_name        NVARCHAR(200) NULL,
        action           NVARCHAR(30)  NOT NULL,
        entity_type      NVARCHAR(30)  NOT NULL,
        entity_id        BIGINT        NOT NULL,
        entity_name      NVARCHAR(300) NULL,
        old_values       NVARCHAR(MAX) NULL,
        new_values       NVARCHAR(MAX) NULL,
        description      NVARCHAR(1000) NULL,
        ip_address       NVARCHAR(45)  NULL,
        user_agent       NVARCHAR(500) NULL,
        project_id       BIGINT        NULL,
        project_name     NVARCHAR(200) NULL,
        created_at       DATETIME2(0)  NOT NULL CONSTRAINT df_audit_logs_created DEFAULT SYSUTCDATETIME(),
        CONSTRAINT fk_audit_logs_organization FOREIGN KEY (organization_id) REFERENCES dbo.organizations(id)
    );
END
GO

IF OBJECT_ID(N'dbo.audit_logs', N'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'ix_audit_logs_organization_id' AND object_id = OBJECT_ID(N'dbo.audit_logs'))
        CREATE INDEX ix_audit_logs_organization_id ON dbo.audit_logs (organization_id);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_audit_logs_org_created' AND object_id = OBJECT_ID(N'dbo.audit_logs'))
        CREATE INDEX IX_audit_logs_org_created ON dbo.audit_logs (organization_id, created_at DESC);

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_audit_logs_org_entity' AND object_id = OBJECT_ID(N'dbo.audit_logs'))
        CREATE INDEX IX_audit_logs_org_entity ON dbo.audit_logs (organization_id, entity_type, entity_id);
END
GO

IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'046_audit_logs.sql')
    INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'046_audit_logs.sql');
GO

PRINT '046_audit_logs: completed.';
GO
