-- Stratix v1 — reports (generated analytics exports)
-- Requires: users, projects, departments

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'reports')
BEGIN
    CREATE TABLE reports (
        id              BIGINT IDENTITY(1,1) NOT NULL,
        title           NVARCHAR(300)        NOT NULL,
        report_type     NVARCHAR(50)         NOT NULL,
        format          NVARCHAR(20)         NOT NULL CONSTRAINT DF_reports_format DEFAULT ('PDF'),
        project_id      BIGINT               NULL,
        department_id   BIGINT               NULL,
        employee_id     BIGINT               NULL,
        date_from       DATE                 NULL,
        date_to         DATE                 NULL,
        file_url        NVARCHAR(1000)       NULL,
        generated_by    BIGINT               NOT NULL,
        created_at      DATETIME2(0)         NOT NULL CONSTRAINT DF_reports_created_at DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_reports PRIMARY KEY (id),
        CONSTRAINT CK_reports_type CHECK (report_type IN (
            'PROJECTS_PROGRESS', 'TASKS_STATUS', 'EMPLOYEE_PERFORMANCE',
            'DELAYED_TASKS', 'KPI_SUMMARY', 'CUSTOM'
        )),
        CONSTRAINT CK_reports_format CHECK (format IN ('PDF', 'EXCEL')),
        CONSTRAINT FK_reports_project FOREIGN KEY (project_id)
            REFERENCES projects (id) ON DELETE SET NULL,
        CONSTRAINT FK_reports_department FOREIGN KEY (department_id)
            REFERENCES departments (id) ON DELETE SET NULL,
        CONSTRAINT FK_reports_employee FOREIGN KEY (employee_id)
            REFERENCES users (id) ON DELETE NO ACTION,
        CONSTRAINT FK_reports_generator FOREIGN KEY (generated_by)
            REFERENCES users (id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_reports_type ON reports (report_type);
    CREATE INDEX IX_reports_created ON reports (created_at DESC);
    CREATE INDEX IX_reports_generator ON reports (generated_by);
END;
GO
