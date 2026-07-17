-- Stratix — organization custom roles (tenant-scoped)
-- SQL Server

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'organization_roles')
BEGIN
    CREATE TABLE organization_roles (
        id              BIGINT IDENTITY(1,1) NOT NULL,
        organization_id BIGINT               NOT NULL,
        code            NVARCHAR(80)         NOT NULL,
        name            NVARCHAR(150)        NOT NULL,
        description     NVARCHAR(500)        NULL,
        base_role       NVARCHAR(50)         NOT NULL,
        is_system       BIT                  NOT NULL CONSTRAINT DF_organization_roles_is_system DEFAULT (0),
        created_at      DATETIME2(0)         NOT NULL CONSTRAINT DF_organization_roles_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_organization_roles PRIMARY KEY (id),
        CONSTRAINT UQ_organization_roles_org_code UNIQUE (organization_id, code),
        CONSTRAINT FK_organization_roles_organization
            FOREIGN KEY (organization_id) REFERENCES organizations(id)
    );

    CREATE INDEX IX_organization_roles_organization_id ON organization_roles(organization_id);
END;
GO

IF COL_LENGTH('users', 'organization_role_id') IS NULL
BEGIN
    ALTER TABLE users ADD organization_role_id BIGINT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_users_organization_role'
)
BEGIN
    ALTER TABLE users
        ADD CONSTRAINT FK_users_organization_role
        FOREIGN KEY (organization_role_id) REFERENCES organization_roles(id);
END;
GO
