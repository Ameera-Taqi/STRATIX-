-- Stratix — drop project_milestones (feature removed)

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'project_milestones')
BEGIN
    DROP TABLE project_milestones;
END;
GO
