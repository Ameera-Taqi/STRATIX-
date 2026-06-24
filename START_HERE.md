# STRATIX — Start Here

**Plan. Execute. Achieve.**

Internal enterprise platform — **not** a portfolio site.

---

## Where we are now

| Phase | Topic | Status |
|-------|--------|--------|
| 0 | Vision, colors, wireframes | Done (your spec) |
| 1 | Modules (10) | Done → [docs/MODULES.md](docs/MODULES.md) |
| 2 | User roles (5) | Done → [docs/ROLES.md](docs/ROLES.md) |
| 3 | Database ERD | Done → [docs/ERD.md](docs/ERD.md) + [DATABASE_WORKFLOW.md](docs/DATABASE_WORKFLOW.md) |
| 4 | SQL scripts (10 tables + reports) | Done → [database/schema/](database/schema/) |
| 5 | Backend: `users` + API | Done (dev uses H2 in memory) |
| 6 | Frontend: full wireframe UI (8 pages) | Done → [docs/WIREFRAME.md](docs/WIREFRAME.md) |
| **7** | **SQL Server + real DB** | **← YOU ARE HERE** |
| 8 | Authentication (JWT) | Next |
| 9 | Projects → Stages → Tasks | Next |
| 10 | KPI, Reports, Notifications | Later |

---

## Phase 7 — First real step (database)

Do this **before** more coding.

### 1. Open SQL Server Management Studio (SSMS)

### 2. Create database

```sql
CREATE DATABASE stratix;
GO
USE stratix;
GO
```

### 3. Run scripts **in order**

Open and execute each file from `database/schema/`:

1. `001_departments.sql`
2. `002_users.sql`
3. `003_projects.sql`
4. `004_project_stages.sql`
5. `005_tasks.sql`
6. `006_task_comments.sql`
7. `007_employee_kpis.sql`
8. `008_notifications.sql`
9. `009_project_files.sql`
10. `010_reports.sql`

### 4. Verify

```sql
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
```

You should see **10 tables** (including `reports`).

---

## Phase 8+ — Development order (after DB)

```
Authentication (JWT + login)
    → Departments API
    → Users CRUD (complete)
    → Projects
    → Stages
    → Tasks + board
    → Team / Employees UI
    → Performance & KPI
    → Reports & Analytics
```

---

## Run locally (after setup)

**Terminal 1 — API**

```powershell
.\run-backend.ps1
```

**Terminal 2 — Dashboard**

```powershell
.\run-frontend.ps1
```

| URL | What |
|-----|------|
| http://localhost:4200 | Angular dashboard |
| http://localhost:8080 | Spring Boot API |
| http://localhost:8080/api/health | Health check |

**Dev mode** uses H2 (no SSMS needed). For **SQL Server**, set in PowerShell before `run-backend.ps1`:

```powershell
$env:SPRING_PROFILES_ACTIVE = "sqlserver"
$env:DB_USERNAME = "sa"
$env:DB_PASSWORD = "YourPassword"
```

*(Add `application-sqlserver.yml` when you finish Phase 7.)*

---

## Core tables (reminder)

```
departments → users
users → projects (manager)
projects → project_stages → tasks
tasks → task_comments
users → employee_kpis, notifications
projects → project_files
```

---

## What to do right now

1. Read [docs/MODULES.md](docs/MODULES.md) and [docs/ROLES.md](docs/ROLES.md) (5 min)
2. Open [docs/ERD.md](docs/ERD.md) — confirm relationships
3. Run SQL scripts in SSMS (Phase 7)
4. Tell me when DB is ready → we continue with **JWT + login**
