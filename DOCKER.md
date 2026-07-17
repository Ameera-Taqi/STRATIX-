# Stratix — Docker deployment (Microsoft SQL Server 2022)

## Architecture

```
┌─────────────────┐     ┌──────────────────┐     ┌─────────────────┐
│  sqlserver-init │────▶│    sqlserver     │◀────│   stratix-api   │
│   (one-shot)    │     │  SQL Server 2022 │     │  Spring Boot 3  │
└─────────────────┘     │   StratixDB      │     └─────────────────┘
                        └──────────────────┘
                              :1433                :8080
```

| Service | Image | Purpose |
|---------|-------|---------|
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest` | Database engine |
| `sqlserver-init` | same (one-shot) | Creates `StratixDB` + baseline schema |
| `stratix-api` | built from `backend/Dockerfile` | REST API + Hibernate |

## Quick start

```bash
# 1. Configure environment (optional)
cp .env.example .env

# 2. Start all services
docker compose up -d --build

# 3. Verify SQL Server
docker compose logs sqlserver-init
docker compose exec sqlserver /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P 'YourStrong!Passw0rd' -C \
  -Q "SELECT name FROM sys.databases WHERE name = N'StratixDB'"

# 4. Verify Spring Boot + database connection
curl -s http://localhost:8080/api/health | python3 -m json.tool
# Expected: "databaseProduct": "Microsoft SQL Server", "database": "UP"

# 5. Login
curl -s -X POST http://localhost:8080/api/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"1234"}'
```

## Default credentials

| Component | User | Password |
|-----------|------|----------|
| SQL Server SA | `sa` | `YourStrong!Passw0rd` (override via `MSSQL_SA_PASSWORD`) |
| App login (seed) | `superadmin` / `admin` | `1234` |

> SQL Server requires a strong SA password (8+ chars, upper, lower, digit, symbol).

## Configuration files

| File | Role |
|------|------|
| `docker-compose.yml` | Service orchestration |
| `backend/src/main/resources/application.properties` | JDBC + JPA (SQL Server) |
| `backend/src/main/resources/application-docker.yml` | Docker profile overrides |
| `database/init/sqlserver/00-create-database.sql` | Creates `StratixDB` |
| `database/init/sqlserver/01-schema.sql` | Optional baseline tables |
| `database/docker/sqlserver/init-db.sh` | Init container entrypoint |

## Schema management

- **Docker default:** `JPA_DDL_AUTO=update` — Hibernate creates/updates tables on startup.
- **Production:** set `JPA_DDL_AUTO=validate` and apply `database/init/sqlserver/01-schema.sql` manually.

## Useful commands

```bash
docker compose ps
docker compose logs -f stratix-api
docker compose down          # stop
docker compose down -v       # stop + delete DB volume (fresh start)
```

## Local development without Docker

```bash
# H2 in-memory (no SQL Server)
cd backend && mvn spring-boot:run -Dspring-boot.run.profiles=dev

# Native SQL Server on localhost:1433
cd backend && mvn spring-boot:run -Dspring-boot.run.profiles=sqlserver
```

## Frontend

```bash
cd frontend && npx ng serve --port 4200
# API proxied to http://localhost:8080
```
