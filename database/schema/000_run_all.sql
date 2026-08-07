-- Stratix — run all SQL Server migrations in order (SSMS or sqlcmd)
-- Single source of truth for schema. Docker init applies this same sequence.
--
-- Usage (from database/schema):
--   sqlcmd -S localhost,1433 -U sa -P '...' -C -I -d StratixDB -i 000_run_all.sql
--
-- Legacy (excluded): 007_employee_kpis.sql, 008_notifications.sql, 009_project_files.sql
--   → superseded by 021_wave2_entities.sqlserver.sql (create + upgrade)

:r 001_departments.sql
:r 002_users.sql
:r 003_projects.sql
:r 004_project_stages.sql
:r 005_tasks.sql
:r 006_task_comments.sql
:r 010_reports.sql
:r 011_project_risks.sqlserver.sql
:r 012_status_lookups.sqlserver.sql
:r 014_drop_project_milestones.sqlserver.sql
:r 015_password_reset_tokens.sqlserver.sql
:r 016_multitenancy.sqlserver.sql
:r 017_subscription_plans.sqlserver.sql
:r 018_subscriptions.sqlserver.sql
:r 019_refresh_tokens.sqlserver.sql
:r 020_login_lockout.sqlserver.sql
:r 021_wave2_entities.sqlserver.sql
:r 022_task_comments_org.sql
:r 023_platform_module_permissions.sql
:r 024_organization_logo.sql
:r 025_organization_roles.sql
:r 026_drop_change_requests.sql
:r 027_project_file_details.sql
:r 028_soft_delete.sql
:r 029_plan_truth_and_token_org.sql
:r 030_reports_module.sql
:r 031_employee_kpi_unique_period.sql
:r 032_tenant_indexes_and_user_soft_delete.sql
:r 033_global_unique_email.sql
:r 034_refresh_token_reuse_detection.sql
:r 035_schema_migrations.sql
:r 036_progress_health_execution.sql
:r 037_kpi_evaluation_framework.sql
:r 038_backfill_estimated_hours_default.sql
:r 039_evaluation_reopen_reason.sql
:r 040_kpi_period_definition_snapshots.sql
:r 041_kpi_weight_total_and_role_scope.sql
