-- Stratix — Soft Delete columns + filtered unique indexes
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- Helper: add soft-delete columns if missing
IF COL_LENGTH('departments', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE departments ADD is_deleted BIT NOT NULL CONSTRAINT DF_departments_is_deleted DEFAULT 0;
    ALTER TABLE departments ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('organization_roles', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE organization_roles ADD is_deleted BIT NOT NULL CONSTRAINT DF_organization_roles_is_deleted DEFAULT 0;
    ALTER TABLE organization_roles ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('projects', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE projects ADD is_deleted BIT NOT NULL CONSTRAINT DF_projects_is_deleted DEFAULT 0;
    ALTER TABLE projects ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('project_stages', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE project_stages ADD is_deleted BIT NOT NULL CONSTRAINT DF_project_stages_is_deleted DEFAULT 0;
    ALTER TABLE project_stages ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('tasks', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE tasks ADD is_deleted BIT NOT NULL CONSTRAINT DF_tasks_is_deleted DEFAULT 0;
    ALTER TABLE tasks ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('task_comments', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE task_comments ADD is_deleted BIT NOT NULL CONSTRAINT DF_task_comments_is_deleted DEFAULT 0;
    ALTER TABLE task_comments ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('project_risks', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE project_risks ADD is_deleted BIT NOT NULL CONSTRAINT DF_project_risks_is_deleted DEFAULT 0;
    ALTER TABLE project_risks ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('project_files', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE project_files ADD is_deleted BIT NOT NULL CONSTRAINT DF_project_files_is_deleted DEFAULT 0;
    ALTER TABLE project_files ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('notifications', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE notifications ADD is_deleted BIT NOT NULL CONSTRAINT DF_notifications_is_deleted DEFAULT 0;
    ALTER TABLE notifications ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

IF COL_LENGTH('employee_kpis', 'is_deleted') IS NULL
BEGIN
    ALTER TABLE employee_kpis ADD is_deleted BIT NOT NULL CONSTRAINT DF_employee_kpis_is_deleted DEFAULT 0;
    ALTER TABLE employee_kpis ADD deleted_at DATETIMEOFFSET NULL;
END;
GO

-- Replace unique constraints with filtered unique indexes (active rows only)
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_departments_name' AND parent_object_id = OBJECT_ID(N'departments'))
    ALTER TABLE departments DROP CONSTRAINT UQ_departments_name;
GO
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_departments_org_name' AND parent_object_id = OBJECT_ID(N'departments'))
    ALTER TABLE departments DROP CONSTRAINT UQ_departments_org_name;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_departments_org_name_active' AND object_id = OBJECT_ID(N'departments'))
BEGIN
    CREATE UNIQUE INDEX UX_departments_org_name_active
        ON departments (organization_id, name)
        WHERE is_deleted = 0;
END;
GO

IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = N'UQ_organization_roles_org_code' AND parent_object_id = OBJECT_ID(N'organization_roles'))
    ALTER TABLE organization_roles DROP CONSTRAINT UQ_organization_roles_org_code;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_organization_roles_org_code_active' AND object_id = OBJECT_ID(N'organization_roles'))
BEGIN
    CREATE UNIQUE INDEX UX_organization_roles_org_code_active
        ON organization_roles (organization_id, code)
        WHERE is_deleted = 0;
END;
GO
