-- Stratix v1 — departments (run first)
-- SQL Server

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'departments')
BEGIN
    CREATE TABLE departments (
        id          BIGINT IDENTITY(1,1) NOT NULL,
        name        NVARCHAR(150)        NOT NULL,
        description NVARCHAR(500)        NULL,
        created_at  DATETIME2(0)         NOT NULL CONSTRAINT DF_departments_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_departments PRIMARY KEY (id),
        CONSTRAINT UQ_departments_name UNIQUE (name)
    );
END;
GO
