# Stratix database

## Source of truth

| Path | Role |
|------|------|
| [`schema/`](schema/) | **Only** schema source — numbered SQL Server migrations |
| [`schema/000_run_all.sql`](schema/000_run_all.sql) | Ordered runner for SSMS / sqlcmd |
| [`init/sqlserver/00-create-database.sql`](init/sqlserver/00-create-database.sql) | Creates `StratixDB` (Docker / manual) |
| [`docker/sqlserver/init-db.sh`](docker/sqlserver/init-db.sh) | Docker one-shot: create DB + apply `schema/` in order |

`init/sqlserver/01-schema.sql` is **deprecated** (stub only).

## Fresh install

```bash
docker compose up -d
# sqlserver-init runs 00-create-database.sql then schema 001…028
```

Or manually:

```bash
sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -C -d master \
  -v DatabaseName=StratixDB -i database/init/sqlserver/00-create-database.sql

sqlcmd -S localhost,1433 -U sa -P 'YourStrong!Passw0rd' -C -I -d StratixDB \
  -i database/schema/000_run_all.sql
```

## Legacy scripts

`007_employee_kpis.sql`, `008_notifications.sql`, `009_project_files.sql` are **v1 shapes**.  
They are excluded from `000_run_all.sql`. Modern create + upgrade is `021_wave2_entities.sqlserver.sql`.

## Upgrade path (existing DBs)

Re-run newer scripts idempotently (or re-run init on a new volume):

- `021` — upgrade KPI / notifications / project_files columns
- `027` — file description / category
- `028` — soft-delete columns + filtered unique indexes
- `029` — drop global `departments.name` unique; sync plan mirror; token `organization_id`
- `030` — activate Reports module (OrganizationId, file storage metadata, soft delete)
- `031` — unique KPI per `(organization_id, user_id, period)` (active rows)
