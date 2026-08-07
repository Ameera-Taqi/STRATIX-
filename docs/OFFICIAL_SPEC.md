# STRATIX — المواصفات الرسمية (Official Spec)

> **وثيقة مرجعية** — يجب أن تطابق التنفيذ الحالي (.NET 8 + Angular 19).  
> تفاصيل الأدوار: [ROLES.md](ROLES.md) · الوحدات: [MODULES.md](MODULES.md)

---

## 1. Stack

| Layer | Tech |
|-------|------|
| API | ASP.NET Core 8 (Clean Architecture: Api / Application / Domain / Infrastructure) |
| Web | Angular 19 + Tailwind |
| DB | Microsoft SQL Server 2022 |
| Auth | JWT + refresh tokens + named Authorization Policies |
| Tenancy | Shared DB + `OrganizationId` query filters + insert stamp |

---

## 2. Stratix Modules

Core product modules (10) plus platform extensions (`AUDIT`, `RISKS`, `ORGANIZATIONS`, `PLATFORM_CMS`, `ROLES`, `BRANDING`).  
Full table: [MODULES.md](MODULES.md) · ACL: `stratix-modules.ts`.

Project workspace tabs: Overview → Timeline → Backlog → Board → List → Files → Activity.

---

## 3. User Roles (7)

| # | Role | Code | Scope |
|---|------|------|--------|
| 1 | Super Admin | `SUPER_ADMIN` | Platform — all organizations |
| 2 | Organization Admin | `ORG_ADMIN` | Full access within one tenant |
| 3 | Admin | `ADMIN` | Tenant admin |
| 4 | Project Manager | `PROJECT_MANAGER` | Projects / features / tasks / risks |
| 5 | Team Leader | `TEAM_LEADER` | Assign / lead tasks |
| 6 | Employee | `EMPLOYEE` | Own tasks |
| 7 | Executive Viewer | `EXECUTIVE_VIEWER` | Read dashboards / reports |

- Stored in: `users.role` (SQL + `Stratix.Domain.Enums.UserRole`)
- JWT claims: `userId`, `role`, `email`, `orgId`, `name`

---

## 4. Persistence policies

| Policy | Behavior |
|--------|----------|
| Tenant stamp | On insert, scoped users always get `OrganizationId` from JWT (cannot override). Unscoped writers must set it or fall back to JWT `orgId`. Updates freeze `OrganizationId`. |
| Soft delete | `ISoftDeletable` entities convert `Remove` → `IsDeleted` + `DeletedAt`; hidden by query filters. Organizations use status `CANCELLED` instead. |
| Audit timestamps | `CreatedAt` / `UpdatedAt` auto-stamped in `SaveChanges`. Domain audit trail via `IAuditTrailService` (CREATE / UPDATE / DELETE). |

---

## 5. Code references

| Layer | Path |
|-------|------|
| Backend roles | `backend/src/Stratix.Domain/Enums/DomainEnums.cs` |
| Auth policies | `backend/src/Stratix.Api/Auth/AuthPolicies.cs` |
| DbContext + filters | `backend/src/Stratix.Infrastructure/Persistence/StratixDbContext.cs` |
| EF configurations | `backend/src/Stratix.Infrastructure/Persistence/Configurations/` |
| Frontend modules | `frontend/src/app/core/config/stratix-modules.ts` |
| Frontend roles | `frontend/src/app/core/config/stratix-roles.ts` |

---

## 6. Database

- [database/README.md](../database/README.md) — bootstrap / source of truth
- [ERD.md](ERD.md) — schema overview
- Scripts: `database/schema/001`–`028` via `000_run_all.sql`
- Docker init applies the same sequence (not the deprecated `01-schema.sql`)

## 7. Workflow

```
Create Project → Add Features → Add Tasks → Assign Employees
    → Track Progress → Evaluate Performance → Generate Reports
```
