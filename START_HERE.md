# STRATIX — Start Here

**Plan. Execute. Achieve.**

Multi-tenant enterprise platform (ASP.NET Core 8 + Angular 19 + SQL Server).

---

## Stack (current)

| Layer | Tech |
|-------|------|
| API | ASP.NET Core 8 (Clean Architecture) |
| Web | Angular 19 + Tailwind |
| DB | Microsoft SQL Server 2022 |
| Auth | JWT + refresh tokens |
| Roles | 7 roles incl. `SUPER_ADMIN`, `ORG_ADMIN` |

---

## Run locally

```bash
# Full stack (API + SQL Server + Mailpit)
docker compose up -d --build

# Frontend (separate terminal)
cd frontend && npm start
```

| URL | What |
|-----|------|
| http://localhost:4200 | Angular dashboard |
| http://localhost:8080 | API |
| http://localhost:8080/api/health | Health check |
| http://localhost:8025 | Mailpit (dev mail) |

### Seed logins (password `1234`)

| Login | Role |
|-------|------|
| `superadmin` | Platform Super Admin |
| `admin` | Tenant Admin |
| `sara` | Project Manager |
| `employee` | Employee |

---

## Docs

| Doc | Topic |
|-----|--------|
| [docs/ROLES.md](docs/ROLES.md) | RBAC roles |
| [docs/MODULES.md](docs/MODULES.md) | Modules |
| [docs/ERD.md](docs/ERD.md) | Data model |
| [backend/README.md](backend/README.md) | API |
| [DOCKER.md](DOCKER.md) | Docker |

---

## Core domain

```
organizations → subscriptions / plans
organizations → departments → users
organizations → projects → features / tasks / risks
tasks → task_comments
users → employee_kpis, notifications
projects → project_files
```
