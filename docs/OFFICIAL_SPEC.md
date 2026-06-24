# STRATIX — المواصفات الرسمية (Official Spec)

> **وثيقة مرجعية ثابتة** — أي تطوير جديد يجب أن يلتزم بهذه الأقسام والأدوار.

---

## 1. Stratix Modules (رسمي — 10 وحدات)

| # | Module (EN) | الوحدة (AR) | Code | Route / UI |
|---|-------------|-------------|------|------------|
| 1 | **Authentication** | المصادقة | `AUTH` | `/auth/login` |
| 2 | **Dashboard** | لوحة التحكم | `DASHBOARD` | `/dashboard` |
| 3 | **Projects** | المشاريع | `PROJECTS` | `/projects` |
| 4 | **Stages** | المراحل | `STAGES` | `/projects/:id` (tab Stages) |
| 5 | **Tasks** | المهام | `TASKS` | `/tasks` |
| 6 | **Employees** | الموظفون | `EMPLOYEES` | `/team` |
| 7 | **Performance & KPI** | الأداء ومؤشرات KPI | `PERFORMANCE` | `/performance` |
| 8 | **Reports & Analytics** | التقارير والتحليلات | `REPORTS` | `/reports` |
| 9 | **Notifications** | الإشعارات | `NOTIFICATIONS` | `/notifications` |
| 10 | **Settings** | الإعدادات | `SETTINGS` | `/settings` |

### ملاحظات تنفيذية

- **Stages (4)** ليست صفحة مستقلة في الـ sidebar؛ منطقها ضمن **Project Details** (تبويب المراحل).
- **Authentication (1)** بوابة الدخول قبل الوصول لباقي الوحدات (JWT + RBAC).
- API prefix مقترح: `/api/v1/{module-code}/...`

---

## 2. User Roles (رسمي — 5 أدوار)

| # | Role (EN) | الدور (AR) | DB / JWT Code |
|---|-----------|------------|---------------|
| 1 | **Admin** | مدير النظام | `ADMIN` |
| 2 | **Project Manager** | مدير المشروع | `PROJECT_MANAGER` |
| 3 | **Team Leader** | قائد الفريق | `TEAM_LEADER` |
| 4 | **Employee** | موظف | `EMPLOYEE` |
| 5 | **Executive Viewer** | مشاهد تنفيذي | `EXECUTIVE_VIEWER` |

- مخزّن في: `users.role` (SQL Server `CHECK` + Java `UserRole` enum).
- JWT claims: `userId`, `role`, `email`.

تفاصيل الصلاحيات: [ROLES.md](ROLES.md)

---

## 3. العلاقة بين الوحدات والأدوار

```
Authentication → يحمي كل الوحدات
Dashboard      → قراءة لجميع الأدوار (حسب الصلاحيات)
Projects       → Admin, PM (كتابة) | Team Leader, Employee (محدود) | Executive (قراءة)
Stages         → تبع Projects
Tasks          → Admin, PM, Team Leader (إدارة) | Employee (مهامه) | Executive (قراءة)
Employees      → Admin (كامل) | PM, Team Leader (عرض فريق) | Executive (قراءة)
Performance    → Admin, PM, Team Leader, Executive
Reports        → Admin, PM, Team Leader, Executive
Notifications  → جميع المستخدمين المسجّلين
Settings       → Admin (نظام) | كل مستخدم (ملفه الشخصي لاحقاً)
```

---

## 4. مراجع الكود

| طبقة | الملف |
|------|--------|
| Backend roles | `backend/.../enums/UserRole.java` |
| Backend modules | `backend/.../enums/SystemModule.java` |
| Frontend modules | `frontend/.../config/stratix-modules.ts` |
| Frontend roles | `frontend/.../config/stratix-roles.ts` |

---

## 5. Database (ERD)

- [ERD.md](ERD.md) — مخطط الجداول والعلاقات
- [DATABASE_WORKFLOW.md](DATABASE_WORKFLOW.md) — Workflow ↔ SQL
- Scripts: `database/schema/001`–`010`

## 6. Workflow الرسمي

```
Create Project → Add Stages → Add Tasks → Assign Employees
    → Track Progress → Evaluate Performance → Generate Reports
```
