# Stratix — Docker deployment (Microsoft SQL Server 2022)

## Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  sqlserver-init │────▶│    sqlserver     │◀────│   stratix-api   │
│   (one-shot)    │     │  SQL Server 2022 │     │  ASP.NET Core 8 │
└─────────────────┘     │   StratixDB      │     └─────────────────┘
                        └──────────────────┘
                              :1433                :8080
```

| Service | Image | Purpose |
|---------|-------|---------|
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | Database engine |
| `sqlserver-init` | same (one-shot) | Creates `StratixDB` + baseline schema |
| `stratix-api` | built from `backend/Dockerfile` | REST API (.NET 8) |
| `mailpit` | `axllent/mailpit` | Local SMTP inbox (`:8025`) |

## Quick start

```bash
# 1. Configure environment (optional)
cp .env.example .env

# 2. Start API + SQL Server + Mailpit
docker compose up -d --build

# 3. Verify SQL Server
docker compose logs sqlserver-init
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Passw0rd' -C \
  -Q "SELECT name FROM sys.databases WHERE name = N'StratixDB'"

# 4. Verify API + database connection
curl -s http://localhost:8080/api/health
# Expected: database UP

# 5. Login
curl -s -X POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"1234"}'
```

## Default credentials

| Component | User | Password |
|-----------|------|----------|
| SQL Server SA | `sa` | `YourStrong!Passw0rd` (override via `MSSQL_SA_PASSWORD`) |
| App login (seed) | `superadmin` / `admin` / `sara` | `1234` |

> SQL Server requires a strong SA password (8+ chars, upper, lower, digit, symbol).

## Configuration files

| File | Role |
|------|------|
| `docker-compose.yml` | Service orchestration |
| `backend/src/Stratix.Api/appsettings.json` | API config (JWT, connection string) |
| `database/schema/` | **Schema source of truth** (`000_run_all.sql`) |
| `database/init/sqlserver/00-create-database.sql` | Creates `StratixDB` |
| `database/docker/sqlserver/init-db.sh` | Init: create DB + apply `schema/` in order |
| `database/README.md` | Database bootstrap notes |

## Schema management

`sqlserver-init` applies the same ordered list as `database/schema/000_run_all.sql` (001→029).  
Do not use the deprecated `init/sqlserver/01-schema.sql` stub. The API maps tables with EF Core; schema changes ship as SQL migrations under `database/schema/`.

### Plan / subscription source of truth

| Concern | Source |
|---------|--------|
| Active plan code + billing status | `subscriptions` (`plan_code`, `status`) |
| Quotas / AI / storage limits | `subscription_plans` (`PlanTier`) |
| `organizations.subscription_plan` | Denormalized mirror only (kept in sync) |

## Tenant file storage

| Concern | Approach |
|---------|----------|
| Persistence | Named volume `stratix_api_data` → `/app/data` inside the API container |
| Public exposure | **Not** mapped with `UseStaticFiles` — downloads only via authenticated APIs |
| AuthZ | Report/logo downloads require a JWT and resolve the file under the caller's `OrganizationId` |
| Content trust | Magic-byte signature checks on upload (PDF / Excel OOXML·OLE / PNG·JPEG·WebP / SVG) |
| Backup | `./scripts/backup-tenant-storage.sh` (archives the Docker volume) |

```bash
# Backup files volume
./scripts/backup-tenant-storage.sh

# Note: `docker compose down -v` also deletes stratix_api_data
```

## Useful commands

```bash
docker compose ps
docker compose logs -f stratix-api
docker compose down          # stop
docker compose down -v       # stop + delete DB volume (fresh start)
```

## Local development

```bash
# Backend stack
docker compose up -d --build

# Frontend
cd frontend && npm start
# UI http://localhost:4200 — API proxied to http://localhost:8080
```
