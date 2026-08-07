-- =============================================================================
-- Stratix — Status lookup tables (PostgreSQL)
-- Run after: 003_projects.sql, 005_tasks.sql, 011_project_risks.sqlserver.sql
--
-- No application code changes required:
--   • Existing columns stay VARCHAR (status)
--   • Values remain the same codes (PLANNED, TODO, OPEN, …)
--   • FK references project_statuses.code / task_statuses.code / risk_statuses.code
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Lookup tables
-- -----------------------------------------------------------------------------

CREATE TABLE IF NOT EXISTS project_statuses (
    id          SMALLSERIAL  PRIMARY KEY,
    code        VARCHAR(30)  NOT NULL UNIQUE,
    label_en    VARCHAR(100) NOT NULL,
    label_ar    VARCHAR(100) NOT NULL,
    sort_order  SMALLINT     NOT NULL DEFAULT 0,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS task_statuses (
    id          SMALLSERIAL  PRIMARY KEY,
    code        VARCHAR(30)  NOT NULL UNIQUE,
    label_en    VARCHAR(100) NOT NULL,
    label_ar    VARCHAR(100) NOT NULL,
    sort_order  SMALLINT     NOT NULL DEFAULT 0,
    is_terminal BOOLEAN      NOT NULL DEFAULT FALSE,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS risk_statuses (
    id          SMALLSERIAL  PRIMARY KEY,
    code        VARCHAR(20)  NOT NULL UNIQUE,
    label_en    VARCHAR(100) NOT NULL,
    label_ar    VARCHAR(100) NOT NULL,
    sort_order  SMALLINT     NOT NULL DEFAULT 0,
    is_active   BOOLEAN      NOT NULL DEFAULT TRUE,
    created_at  TIMESTAMPTZ  NOT NULL DEFAULT NOW()
);

-- -----------------------------------------------------------------------------
-- 2. Seed data (matches backend enums + existing CHECK constraints)
-- -----------------------------------------------------------------------------

INSERT INTO project_statuses (code, label_en, label_ar, sort_order) VALUES
    ('PLANNED',   'Planned',   'مخطط',        1),
    ('ACTIVE',    'Active',    'نشط',         2),
    ('ON_HOLD',   'On Hold',   'معلق',        3),
    ('COMPLETED', 'Completed', 'مكتمل',       4),
    ('CANCELLED', 'Cancelled', 'ملغى',        5)
ON CONFLICT (code) DO NOTHING;

INSERT INTO task_statuses (code, label_en, label_ar, sort_order, is_terminal) VALUES
    ('TODO',        'To Do',       'قيد الانتظار', 1, FALSE),
    ('IN_PROGRESS', 'In Progress', 'قيد التنفيذ',  2, FALSE),
    ('REVIEW',      'Review',      'مراجعة',       3, FALSE),
    ('DONE',        'Done',        'منجزة',        4, TRUE),
    ('BLOCKED',     'Blocked',     'محجوبة',       5, FALSE)
ON CONFLICT (code) DO NOTHING;

INSERT INTO risk_statuses (code, label_en, label_ar, sort_order) VALUES
    ('OPEN',       'Open',       'مفتوحة',    1),
    ('MITIGATING', 'Mitigating', 'قيد المعالجة', 2),
    ('CLOSED',     'Closed',     'مغلقة',     3)
ON CONFLICT (code) DO NOTHING;

-- -----------------------------------------------------------------------------
-- 3. Replace CHECK constraints with FK → lookup.code (same column, same values)
-- -----------------------------------------------------------------------------

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'projects'
    ) THEN
        ALTER TABLE projects DROP CONSTRAINT IF EXISTS ck_projects_status;
        ALTER TABLE projects DROP CONSTRAINT IF EXISTS CK_projects_status;

        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints
            WHERE constraint_name = 'fk_projects_status'
        ) THEN
            ALTER TABLE projects
                ADD CONSTRAINT fk_projects_status
                FOREIGN KEY (status) REFERENCES project_statuses (code);
        END IF;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'tasks'
    ) THEN
        ALTER TABLE tasks DROP CONSTRAINT IF EXISTS ck_tasks_status;
        ALTER TABLE tasks DROP CONSTRAINT IF EXISTS CK_tasks_status;

        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints
            WHERE constraint_name = 'fk_tasks_status'
        ) THEN
            ALTER TABLE tasks
                ADD CONSTRAINT fk_tasks_status
                FOREIGN KEY (status) REFERENCES task_statuses (code);
        END IF;
    END IF;
END $$;

DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.tables
        WHERE table_schema = 'public' AND table_name = 'project_risks'
    ) THEN
        ALTER TABLE project_risks DROP CONSTRAINT IF EXISTS ck_project_risks_status;

        IF NOT EXISTS (
            SELECT 1 FROM information_schema.table_constraints
            WHERE constraint_name = 'fk_project_risks_status'
        ) THEN
            ALTER TABLE project_risks
                ADD CONSTRAINT fk_project_risks_status
                FOREIGN KEY (status) REFERENCES risk_statuses (code);
        END IF;
    END IF;
END $$;

-- -----------------------------------------------------------------------------
-- 4. Helpful indexes
-- -----------------------------------------------------------------------------

CREATE INDEX IF NOT EXISTS ix_project_statuses_active ON project_statuses (is_active, sort_order);
CREATE INDEX IF NOT EXISTS ix_task_statuses_active ON task_statuses (is_active, sort_order);
CREATE INDEX IF NOT EXISTS ix_risk_statuses_active ON risk_statuses (is_active, sort_order);
