# Stratix — User Roles (Official RBAC)

> **المرجع الرسمي الكامل:** [OFFICIAL_SPEC.md](OFFICIAL_SPEC.md)

## الأدوار الأساسية — ثابتة (5)

| # | Role | Code (`users.role`) |
|---|------|-------------------|
| 1 | Admin | `ADMIN` |
| 2 | Project Manager | `PROJECT_MANAGER` |
| 3 | Team Leader | `TEAM_LEADER` |
| 4 | Employee | `EMPLOYEE` |
| 5 | Executive Viewer | `EXECUTIVE_VIEWER` |

## Permission matrix

| Action | Admin | PM | Team Leader | Employee | Executive |
|--------|:-----:|:--:|:-----------:|:--------:|:---------:|
| Manage users / departments | ✓ | — | — | — | — |
| Create / delete projects | ✓ | ✓ | — | — | — |
| Edit project / stages | ✓ | ✓ | partial | — | — |
| Assign tasks | ✓ | ✓ | ✓ | — | — |
| Update own tasks | ✓ | ✓ | ✓ | ✓ | — |
| View dashboards & KPI | ✓ | ✓ | ✓ | limited | ✓ |
| Export reports | ✓ | ✓ | ✓ | — | ✓ |
| System settings | ✓ | — | — | — | — |

## Code references

- Backend enum: `UserRole.java`
- Backend matrix: `RolePermissions.java`
- Frontend: `stratix-roles.ts`
- JWT claims (planned): `userId`, `role`, `email`
