-- Stratix — create application database (SQL Server)
-- Run against master. Variable :DatabaseName is passed via sqlcmd -v DatabaseName=StratixDB

IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$(DatabaseName)')
BEGIN
    DECLARE @sql NVARCHAR(256) = N'CREATE DATABASE [' + N'$(DatabaseName)' + N']';
    EXEC sp_executesql @sql;
    PRINT N'Database $(DatabaseName) created.';
END
ELSE
BEGIN
    PRINT N'Database $(DatabaseName) already exists.';
END
GO
