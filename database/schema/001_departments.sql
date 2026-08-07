-- Stratix v1 — departments (run first)
-- SQL Server
-- Name uniqueness is per-organization (see 028 filtered index UX_departments_org_name_active).
-- Do NOT add a global UNIQUE on name alone — that breaks multi-tenancy.

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'departments')
BEGIN
    CREATE TABLE departments (
        id          BIGINT IDENTITY(1,1) NOT NULL,
        name        NVARCHAR(150)        NOT NULL,
        description NVARCHAR(500)        NULL,
        created_at  DATETIME2(0)         NOT NULL CONSTRAINT DF_departments_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_departments PRIMARY KEY (id)
    );
END;
GO
