-- Stratix — drop change_requests (feature removed)

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'change_requests')
BEGIN
    DROP TABLE change_requests;
END;
GO
