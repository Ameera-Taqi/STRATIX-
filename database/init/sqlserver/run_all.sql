-- Stratix SQL Server — full manual bootstrap (SSMS / sqlcmd against master, then StratixDB)
-- Step 1: run 00-create-database.sql against master
-- Step 2: run 01-schema.sql against StratixDB
-- Or use: docker compose up -d  (automatic via sqlserver-init)

:r 00-create-database.sql
GO
USE StratixDB;
GO
:r 01-schema.sql
GO
