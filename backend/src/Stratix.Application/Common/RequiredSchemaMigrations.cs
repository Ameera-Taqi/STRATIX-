namespace Stratix.Application.Common;

/// <summary>
/// Script basenames that must be recorded in <c>dbo.schema_migrations</c> for readiness.
/// Keep in sync with <c>database/docker/sqlserver/init-db.sh</c> and <c>000_run_all.sql</c>.
/// </summary>
public static class RequiredSchemaMigrations
{
    public static readonly IReadOnlyList<string> All =
    [
        "001_departments.sql",
        "002_users.sql",
        "003_projects.sql",
        "004_project_stages.sql",
        "005_tasks.sql",
        "006_task_comments.sql",
        "010_reports.sql",
        "011_project_risks.sqlserver.sql",
        "012_status_lookups.sqlserver.sql",
        "014_drop_project_milestones.sqlserver.sql",
        "015_password_reset_tokens.sqlserver.sql",
        "016_multitenancy.sqlserver.sql",
        "017_subscription_plans.sqlserver.sql",
        "018_subscriptions.sqlserver.sql",
        "019_refresh_tokens.sqlserver.sql",
        "020_login_lockout.sqlserver.sql",
        "021_wave2_entities.sqlserver.sql",
        "022_task_comments_org.sql",
        "023_platform_module_permissions.sql",
        "024_organization_logo.sql",
        "025_organization_roles.sql",
        "026_drop_change_requests.sql",
        "027_project_file_details.sql",
        "028_soft_delete.sql",
        "029_plan_truth_and_token_org.sql",
        "030_reports_module.sql",
        "031_employee_kpi_unique_period.sql",
        "032_tenant_indexes_and_user_soft_delete.sql",
        "033_global_unique_email.sql",
        "034_refresh_token_reuse_detection.sql",
        "035_schema_migrations.sql",
        "036_progress_health_execution.sql",
        "037_kpi_evaluation_framework.sql",
        "038_backfill_estimated_hours_default.sql",
        "039_evaluation_reopen_reason.sql",
        "040_kpi_period_definition_snapshots.sql",
        "041_kpi_weight_total_and_role_scope.sql",
        "042_organization_onboarding.sql",
        "043_task_review_submission.sql",
        "044_risk_closure.sql",
        "045_notification_context.sql",
        "046_audit_logs.sql",
    ];
}
