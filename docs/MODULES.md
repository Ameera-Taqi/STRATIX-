# Stratix — System Modules (Official)

> **المرجع الرسمي الكامل:** [OFFICIAL_SPEC.md](OFFICIAL_SPEC.md)

**STRATIX** — منصة مؤسسية متعددة المستأجرين (ASP.NET Core 8 + Angular 19 + SQL Server).

## Core modules (product)

| # | Module | Code | Notes |
|---|--------|------|--------|
| 1 | Authentication | `AUTH` | JWT + refresh tokens |
| 2 | Dashboard | `DASHBOARD` | |
| 3 | Projects | `PROJECTS` | Overview / Timeline / Backlog / Board / List / Files / Activity |
| 4 | Features | `STAGES` | Project features (formerly “stages”) |
| 5 | Tasks | `TASKS` | |
| 6 | Employees | `EMPLOYEES` | |
| 7 | Performance & KPI | `PERFORMANCE` | |
| 8 | Reports & Analytics | `REPORTS` | |
| 9 | Notifications | `NOTIFICATIONS` | |
| 10 | Settings | `SETTINGS` | |

## Platform / tenant modules (extended)

| Module | Code |
|--------|------|
| Audit trail | `AUDIT` |
| Risks | `RISKS` |
| Organizations | `ORGANIZATIONS` |
| Platform CMS | `PLATFORM_CMS` |
| Custom roles | `ROLES` |
| Branding | `BRANDING` |

Source of truth for ACL: `frontend/src/app/core/config/stratix-modules.ts`.

## Workflow

```
Create Project → Add Features → Add Tasks → Assign Employees
    → Track Progress → Evaluate Performance → Generate Reports
```

## Removed (do not reintroduce without a decision)

- Project milestones
- Change requests

## Code references

- Backend roles: `Stratix.Domain.Enums.UserRole`
- Frontend modules: `frontend/src/app/core/config/stratix-modules.ts`
- UI map: [WIREFRAME.md](WIREFRAME.md)
- Roles: [ROLES.md](ROLES.md)
