# Stratix .NET API

ASP.NET Core 8 Web API using **Clean Architecture** and **SQL Server**.

## Solution structure

```
backend/
├── Stratix.sln
└── src/
    ├── Stratix.Domain/          # Entities, enums, domain rules
    ├── Stratix.Application/     # DTOs, interfaces, use cases / services
    ├── Stratix.Infrastructure/  # EF Core, SQL Server, JWT, mail, seed data
    └── Stratix.Api/             # Controllers, middleware, composition root
```

Dependency flow: **Api → Infrastructure → Application → Domain**

## Run locally

**Prerequisites:** .NET 8 SDK, SQL Server (or Docker Compose stack)

```bash
# Start SQL Server + API via Docker
cd .. && docker compose up -d sqlserver sqlserver-init mailpit stratix-api

# Or run API only (with local SQL Server)
cd backend
dotnet run --project src/Stratix.Api
```

API listens on **http://localhost:8080**. The Angular frontend proxies `/api` to this port.

## Default seed users

| Login | Password |
|-------|----------|
| `superadmin` or `superadmin@stratix.local` | `1234` |
| `admin` or `admin@stratix.local` | `1234` |
| `sara` or `sara.ali@stratix.local` | `1234` |
| `employee` or `lina.noor@stratix.local` | `1234` |

## Configuration

See `appsettings.json` and `.env.example`. Key settings:

- `ConnectionStrings:DefaultConnection` — SQL Server connection
- `Stratix:Jwt:Secret` — JWT signing key
- `Stratix:FrontendUrl` — password reset links
- `Stratix:SeedData` — seed demo data on startup
