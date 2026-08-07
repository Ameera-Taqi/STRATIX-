-- LEGACY v1 shape — do not include in 000_run_all.sql. Superseded by 021_wave2_entities.sqlserver.sql (create + upgrade).
-- Stratix v1 — employee_kpis

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'employee_kpis')
BEGIN
    CREATE TABLE employee_kpis (
        id                BIGINT IDENTITY(1,1) NOT NULL,
        user_id           BIGINT               NOT NULL,
        project_id        BIGINT               NULL,
        tasks_completed   INT                  NOT NULL CONSTRAINT DF_kpi_tasks_completed DEFAULT (0),
        delayed_tasks     INT                  NOT NULL CONSTRAINT DF_kpi_delayed_tasks DEFAULT (0),
        on_time_rate      DECIMAL(5,2)         NOT NULL CONSTRAINT DF_kpi_on_time DEFAULT (0),
        performance_score DECIMAL(5,2)         NOT NULL CONSTRAINT DF_kpi_score DEFAULT (0),
        evaluation_period NVARCHAR(50)         NOT NULL,

        CONSTRAINT PK_employee_kpis PRIMARY KEY (id),
        CONSTRAINT CK_kpi_on_time CHECK (on_time_rate >= 0 AND on_time_rate <= 100),
        CONSTRAINT CK_kpi_score CHECK (performance_score >= 0 AND performance_score <= 100),
        CONSTRAINT FK_kpi_user FOREIGN KEY (user_id)
            REFERENCES users (id) ON DELETE CASCADE,
        CONSTRAINT FK_kpi_project FOREIGN KEY (project_id)
            REFERENCES projects (id) ON DELETE SET NULL
    );

    CREATE INDEX IX_employee_kpis_user ON employee_kpis (user_id);
    CREATE INDEX IX_employee_kpis_period ON employee_kpis (evaluation_period);
END;
GO
