-- Stratix — project file description / category metadata

IF COL_LENGTH('project_files', 'description') IS NULL
BEGIN
    ALTER TABLE project_files ADD description NVARCHAR(1000) NULL;
END;
GO

IF COL_LENGTH('project_files', 'category') IS NULL
BEGIN
    ALTER TABLE project_files ADD category NVARCHAR(80) NULL;
END;
GO
