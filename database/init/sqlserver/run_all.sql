-- Stratix SQL Server — manual bootstrap pointer
-- Preferred: docker compose up -d  (sqlserver-init applies schema/000_run_all sequence)
--
-- Manual:
--   1) Against master: 00-create-database.sql  (set $(DatabaseName) or edit)
--   2) Against StratixDB: database/schema/000_run_all.sql

:r 00-create-database.sql
GO
PRINT N'Next: run database/schema/000_run_all.sql against StratixDB (not this folder''s old 01-schema.sql).';
GO
