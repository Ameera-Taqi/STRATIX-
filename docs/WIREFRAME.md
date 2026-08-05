# STRATIX — Wireframe & Design System

## Professional color palette

| Token | Hex | Usage |
|-------|-----|--------|
| Primary | `#2563EB` | Buttons, links, active nav, charts |
| Dark | `#0F172A` | Sidebar, headings |
| Background | `#F8FAFC` | Page background |
| Success | `#22C55E` | Active, on-time, done |
| Warning | `#F59E0B` | Delayed, review |
| Danger | `#EF4444` | Critical, blocked, delete |

## Global layout

- **Sidebar:** Logo, Dashboard, Projects, Tasks, Team, Performance, Reports, Settings
- **Topbar:** Search, Notifications, User profile

## Pages (8)

1. **Dashboard** — KPI cards, 3 charts, recent projects table
2. **Projects** — Search, status filter, new project, projects table + actions
3. **Project details** — Header, tabs (Overview, Features, Tasks, Files, Activity)
4. **Tasks board** — To Do | In Progress | Review | Done
5. **Task details** — Info, subtasks, activity
6. **Team** — Search, add employee, employees table
7. **Performance & KPI** — 4 KPI cards, employee performance table
8. **Reports & Analytics** — Filters, 4 charts, PDF/Excel export

## Angular routes

| Route | Page |
|-------|------|
| `/dashboard` | Dashboard |
| `/projects` | Projects list |
| `/projects/:id` | Project details |
| `/tasks` | Kanban board |
| `/tasks/:id` | Task details |
| `/team` | Team / employees |
| `/performance` | Performance & KPI |
| `/reports` | Reports & analytics |
| `/settings` | Settings |

UI implementation: `frontend/src/app/`
