# STRATIX — Project Management System

**Plan. Execute. Achieve.**

> **New to the project?** Read **[START_HERE.md](START_HERE.md)** first.

Internal enterprise platform for projects, tasks, team performance, and analytics.

![Stratix Logo](docs/assets/stratix-logo.png)

## Brand

| Token | Hex |
|-------|-----|
| Primary | `#2563EB` |
| Dark | `#0F172A` |
| Background | `#F8FAFC` |
| Success | `#22C55E` |
| Warning | `#F59E0B` |
| Danger | `#EF4444` |

## Project structure

```
Satrtix/
├── START_HERE.md      ← roadmap & current phase
├── docs/              ← Modules, roles, ERD
├── database/schema/   ← SQL Server scripts (run in SSMS)
├── backend/           ← ASP.NET Core 8 API (Clean Architecture, EF Core)
└── frontend/          ← Angular dashboard
```

## Documentation

- **[Official spec — Modules & Roles](docs/OFFICIAL_SPEC.md)** ← مرجع ثابت
- [START_HERE.md](START_HERE.md) — phases & what to do now
- [Modules](docs/MODULES.md)
- [User roles](docs/ROLES.md)
- [Database ERD](docs/ERD.md)

## Quick run (development)

```powershell
.\run-backend.ps1    # http://localhost:8080
.\run-frontend.ps1   # http://localhost:4200
```

## Workflow

```
Create Project → Add Stages → Add Tasks → Assign Employees
    → Track Progress → Evaluate Performance → Generate Reports
```
