# Stratix — User Roles (RBAC)

> Multi-tenant platform. See also [OFFICIAL_SPEC.md](OFFICIAL_SPEC.md) (historical) and `frontend/src/app/core/config/stratix-roles.ts`.

## Roles (7)

| # | Role | Code | Scope |
|---|------|------|--------|
| 1 | Super Admin | `SUPER_ADMIN` | Platform — all organizations |
| 2 | Organization Admin | `ORG_ADMIN` | Full access within one tenant |
| 3 | Admin | `ADMIN` | Tenant admin (users, departments, settings) |
| 4 | Project Manager | `PROJECT_MANAGER` | Projects, features, tasks, risks |
| 5 | Team Leader | `TEAM_LEADER` | Assign / lead tasks |
| 6 | Employee | `EMPLOYEE` | Own tasks (status updates) |
| 7 | Executive Viewer | `EXECUTIVE_VIEWER` | Read dashboards / reports |

## Permission highlights

| Action | Super | Org Admin | Admin | PM | Leader | Employee | Executive |
|--------|:-----:|:---------:|:-----:|:--:|:------:|:--------:|:---------:|
| Manage all companies | ✓ | — | — | — | — | — | — |
| Manage users / departments | ✓ | ✓ | ✓ | — | — | — | — |
| Create / delete projects | ✓ | ✓ | ✓ | ✓ | — | — | — |
| Assign tasks | ✓ | ✓ | ✓ | ✓ | ✓ | — | — |
| Update **own** task status | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | — |
| View dashboards & KPI | ✓ | ✓ | ✓ | ✓ | ✓ | limited | ✓ |
| Export reports | ✓ | ✓ | ✓ | ✓ | ✓ | — | ✓ |

## Rules enforced in API

- Only `SUPER_ADMIN` may assign `SUPER_ADMIN`
- Only `SUPER_ADMIN` / `ORG_ADMIN` may assign `ORG_ADMIN`
- User write APIs: `SUPER_ADMIN`, `ORG_ADMIN`, `ADMIN` only
- Employees may change status only on tasks assigned to them
- Suspended / cancelled organizations cannot log in (except Super Admin)
- Frontend routes are gated per module via `moduleGuard`

## Code references

- Backend enum: `Stratix.Domain.Enums.UserRole`
- Frontend roles: `stratix-roles.ts`
- Frontend modules / ACL: `stratix-modules.ts`
- JWT claims: `userId`, `role`, `email`, `orgId`, `name`
