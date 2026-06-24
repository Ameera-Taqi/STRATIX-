# Stratix — Database ERD (Official v1)

**SQL Server** — مرجع التصميم قبل أي تطوير Backend إضافي.

Workflow: [DATABASE_WORKFLOW.md](DATABASE_WORKFLOW.md)

---

## Entity Relationship Diagram

```mermaid
erDiagram
    departments ||--o{ users : "employs"
    departments ||--o{ projects : "owns"
    users ||--o{ projects : "manages"
    users ||--o{ tasks : "assigned_to"
    users ||--o{ task_comments : "writes"
    users ||--o{ employee_kpis : "measured"
    users ||--o{ notifications : "receives"
    users ||--o{ project_files : "uploads"
    users ||--o{ reports : "generates"
    projects ||--o{ project_stages : "timeline_stages"
    projects ||--o{ tasks : "has"
    projects ||--o{ employee_kpis : "kpi_scope"
    projects ||--o{ project_files : "attachments"
    projects ||--o{ reports : "filtered_by"
    project_stages ||--o{ tasks : "groups"
    tasks ||--o{ task_comments : "discussed_in"
    departments ||--o{ reports : "filtered_by"
    users ||--o{ reports : "filtered_employee"

    departments {
        bigint id PK
        nvarchar name UK
        nvarchar description
        datetime2 created_at
    }

    users {
        bigint id PK
        nvarchar name
        nvarchar email UK
        nvarchar password
        nvarchar role
        nvarchar job_title
        nvarchar status
        bigint department_id FK
        datetime2 created_at
        datetime2 updated_at
    }

    projects {
        bigint id PK
        nvarchar name
        nvarchar description
        nvarchar status
        nvarchar priority
        date start_date
        date end_date
        decimal progress
        bigint project_manager_id FK
        bigint department_id FK
        datetime2 created_at
    }

    project_stages {
        bigint id PK
        bigint project_id FK
        nvarchar name
        nvarchar description
        nvarchar status
        date start_date
        date end_date
        decimal progress
        int order_number
    }

    tasks {
        bigint id PK
        bigint project_id FK
        bigint stage_id FK
        nvarchar title
        nvarchar description
        nvarchar status
        nvarchar priority
        bigint assigned_to FK
        date start_date
        date due_date
        datetime2 completed_at
        decimal progress
    }

    task_comments {
        bigint id PK
        bigint task_id FK
        bigint user_id FK
        nvarchar comment
        datetime2 created_at
    }

    employee_kpis {
        bigint id PK
        bigint user_id FK
        bigint project_id FK
        int tasks_completed
        int delayed_tasks
        decimal on_time_rate
        decimal performance_score
        nvarchar evaluation_period
    }

    notifications {
        bigint id PK
        bigint user_id FK
        nvarchar title
        nvarchar message
        bit is_read
        datetime2 created_at
    }

    project_files {
        bigint id PK
        bigint project_id FK
        bigint uploaded_by FK
        nvarchar file_name
        nvarchar file_url
        datetime2 created_at
    }

    reports {
        bigint id PK
        nvarchar title
        nvarchar report_type
        nvarchar format
        bigint project_id FK
        bigint department_id FK
        bigint employee_id FK
        date date_from
        date date_to
        nvarchar file_url
        bigint generated_by FK
        datetime2 created_at
    }
```

---

## العلاقات الأساسية (4 + باقي النظام)

| العلاقة | من | إلى | FK |
|---------|-----|-----|-----|
| **Project → Stages** | `projects` | `project_stages` | `project_id` |
| **Stage → Tasks** | `project_stages` | `tasks` | `stage_id` |
| **Task → Employee** | `tasks` | `users` | `assigned_to` |
| **Employee → KPI** | `users` | `employee_kpis` | `user_id` |
| **Project → Timeline** | `projects` + `project_stages` | — | تواريخ + `order_number` |
| Department → Users | `departments` | `users` | `department_id` |
| Project → Tasks | `projects` | `tasks` | `project_id` |
| Task → Comments | `tasks` | `task_comments` | `task_id` |
| Project → Files | `projects` | `project_files` | `project_id` |
| User → Notifications | `users` | `notifications` | `user_id` |
| User → Reports | `users` | `reports` | `generated_by` |

---

## Workflow ↔ Tables

```
Create Project      →  projects
Add Stages          →  project_stages
Add Tasks           →  tasks
Assign Employees    →  tasks.assigned_to → users.id
Track Progress      →  tasks, project_stages, projects (progress %)
Evaluate Performance→  employee_kpis
Generate Reports    →  reports
```

---

## Enum values

| Column | Values |
|--------|--------|
| `users.role` | `ADMIN`, `PROJECT_MANAGER`, `TEAM_LEADER`, `EMPLOYEE`, `EXECUTIVE_VIEWER` |
| `users.status` | `ACTIVE`, `INACTIVE`, `SUSPENDED` |
| `projects.status` | `PLANNED`, `ACTIVE`, `ON_HOLD`, `COMPLETED`, `CANCELLED` |
| `projects.priority` | `LOW`, `MEDIUM`, `HIGH`, `CRITICAL` |
| `project_stages.status` / `tasks.status` | `TODO`, `IN_PROGRESS`, `REVIEW`, `DONE`, `BLOCKED` |
| `tasks.priority` | `LOW`, `MEDIUM`, `HIGH`, `URGENT` |
| `reports.report_type` | `PROJECTS_PROGRESS`, `TASKS_STATUS`, `EMPLOYEE_PERFORMANCE`, `DELAYED_TASKS`, `KPI_SUMMARY`, `CUSTOM` |
| `reports.format` | `PDF`, `EXCEL` |

---

## Script order

`database/schema/001` … `010_reports.sql` — see [DATABASE_WORKFLOW.md](DATABASE_WORKFLOW.md)
