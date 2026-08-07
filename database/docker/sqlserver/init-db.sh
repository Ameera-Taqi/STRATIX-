#!/usr/bin/env bash
# Stratix — one-shot SQL Server database initialization (Docker Compose)
# Creates StratixDB, then applies database/schema migrations in order
# (same sequence as schema/000_run_all.sql).
set -eu

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
HOST="${MSSQL_HOST:-sqlserver}"
SA_PASSWORD="${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}"
DATABASE="${MSSQL_DATABASE:-StratixDB}"
CREATE_DB_SCRIPT="${CREATE_DB_SCRIPT:-/scripts/00-create-database.sql}"
SCHEMA_DIR="${SCHEMA_DIR:-/scripts/schema}"
MAX_ATTEMPTS=30
SLEEP_SECONDS=5

# Keep in sync with database/schema/000_run_all.sql
SCHEMA_SCRIPTS=(
  001_departments.sql
  002_users.sql
  003_projects.sql
  004_project_stages.sql
  005_tasks.sql
  006_task_comments.sql
  010_reports.sql
  011_project_risks.sqlserver.sql
  012_status_lookups.sqlserver.sql
  014_drop_project_milestones.sqlserver.sql
  015_password_reset_tokens.sqlserver.sql
  016_multitenancy.sqlserver.sql
  017_subscription_plans.sqlserver.sql
  018_subscriptions.sqlserver.sql
  019_refresh_tokens.sqlserver.sql
  020_login_lockout.sqlserver.sql
  021_wave2_entities.sqlserver.sql
  022_task_comments_org.sql
  023_platform_module_permissions.sql
  024_organization_logo.sql
  025_organization_roles.sql
  026_drop_change_requests.sql
  027_project_file_details.sql
  028_soft_delete.sql
  029_plan_truth_and_token_org.sql
  030_reports_module.sql
  031_employee_kpi_unique_period.sql
  032_tenant_indexes_and_user_soft_delete.sql
  033_global_unique_email.sql
  034_refresh_token_reuse_detection.sql
  035_schema_migrations.sql
  036_progress_health_execution.sql
  037_kpi_evaluation_framework.sql
  038_backfill_estimated_hours_default.sql
  039_evaluation_reopen_reason.sql
  040_kpi_period_definition_snapshots.sql
  041_kpi_weight_total_and_role_scope.sql
  042_organization_onboarding.sql
  043_task_review_submission.sql
  044_risk_closure.sql
  045_notification_context.sql
)

echo "[stratix-init] Waiting for SQL Server at ${HOST}..."
attempt=1
while [ "${attempt}" -le "${MAX_ATTEMPTS}" ]; do
  if ${SQLCMD} -S "${HOST}" -U sa -P "${SA_PASSWORD}" -C -Q "SELECT 1" -b -o /dev/null 2>/dev/null; then
    echo "[stratix-init] SQL Server is ready (attempt ${attempt})."
    break
  fi
  if [ "${attempt}" -eq "${MAX_ATTEMPTS}" ]; then
    echo "[stratix-init] ERROR: SQL Server did not become ready in time." >&2
    exit 1
  fi
  attempt=$((attempt + 1))
  sleep "${SLEEP_SECONDS}"
done

echo "[stratix-init] Creating database '${DATABASE}' if not exists..."
${SQLCMD} -S "${HOST}" -U sa -P "${SA_PASSWORD}" -C -d master \
  -v DatabaseName="${DATABASE}" \
  -i "${CREATE_DB_SCRIPT}"

if [ ! -d "${SCHEMA_DIR}" ]; then
  echo "[stratix-init] ERROR: schema directory not found: ${SCHEMA_DIR}" >&2
  exit 1
fi

record_migration() {
  local script="$1"
  ${SQLCMD} -S "${HOST}" -U sa -P "${SA_PASSWORD}" -C -I -b -d "${DATABASE}" -Q \
    "IF OBJECT_ID(N'dbo.schema_migrations', N'U') IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.schema_migrations WHERE migration_id = N'${script}')
     INSERT INTO dbo.schema_migrations (migration_id) VALUES (N'${script}');" \
    || true
}

echo "[stratix-init] Applying schema migrations from ${SCHEMA_DIR}..."
for script in "${SCHEMA_SCRIPTS[@]}"; do
  path="${SCHEMA_DIR}/${script}"
  if [ ! -f "${path}" ]; then
    echo "[stratix-init] ERROR: missing migration: ${path}" >&2
    exit 1
  fi
  echo "[stratix-init] -> ${script}"
  ${SQLCMD} -S "${HOST}" -U sa -P "${SA_PASSWORD}" -C -I -b -d "${DATABASE}" -i "${path}"
  record_migration "${script}"
done

echo "[stratix-init] Database initialization completed successfully."
