# Stratix — System Modules (Official)

> **المرجع الرسمي الكامل:** [OFFICIAL_SPEC.md](OFFICIAL_SPEC.md)

**STRATIX** — منصة مؤسسية داخلية (ليست portfolio).

## Stratix Modules — ثابتة (10)

| # | Module | Code |
|---|--------|------|
| 1 | Authentication | `AUTH` |
| 2 | Dashboard | `DASHBOARD` |
| 3 | Projects | `PROJECTS` |
| 4 | Stages | `STAGES` |
| 5 | Tasks | `TASKS` |
| 6 | Employees | `EMPLOYEES` |
| 7 | Performance & KPI | `PERFORMANCE` |
| 8 | Reports & Analytics | `REPORTS` |
| 9 | Notifications | `NOTIFICATIONS` |
| 10 | Settings | `SETTINGS` |

لا تُضاف وحدات جديدة دون تحديث هذه الوثيقة + `SystemModule.java` + `stratix-modules.ts`.

## Workflow

```
Create Project → Add Stages → Add Tasks → Assign Employees
    → Track Progress → Evaluate Performance → Generate Reports
```

## Code references

- Backend: `com.stratix.domain.enums.SystemModule`
- Frontend: `frontend/src/app/core/config/stratix-modules.ts`
- UI map: [WIREFRAME.md](WIREFRAME.md)
