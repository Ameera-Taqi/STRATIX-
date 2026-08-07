-- DEPRECATED — do not use as the schema source of truth.
--
-- Stratix schema lives in: database/schema/ (see 000_run_all.sql).
-- Docker init applies those migrations via database/docker/sqlserver/init-db.sh.
--
-- Manual bootstrap:
--   1) sqlcmd ... -d master -i database/init/sqlserver/00-create-database.sql
--   2) sqlcmd ... -d StratixDB -i database/schema/000_run_all.sql
--      (or run init-db.sh / docker compose up)

PRINT N'Skipped: 01-schema.sql is deprecated. Apply database/schema/000_run_all.sql instead.';
GO
