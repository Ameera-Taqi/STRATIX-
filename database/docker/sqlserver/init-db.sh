#!/usr/bin/env bash
# Stratix — one-shot SQL Server database initialization (Docker Compose)
set -eu

SQLCMD="/opt/mssql-tools18/bin/sqlcmd"
HOST="${MSSQL_HOST:-sqlserver}"
SA_PASSWORD="${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD is required}"
DATABASE="${MSSQL_DATABASE:-StratixDB}"
SCRIPTS_DIR="/scripts/sql"
MAX_ATTEMPTS=30
SLEEP_SECONDS=5

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
  -i "${SCRIPTS_DIR}/00-create-database.sql"

if [ -f "${SCRIPTS_DIR}/01-schema.sql" ]; then
  echo "[stratix-init] Applying optional schema script..."
  ${SQLCMD} -S "${HOST}" -U sa -P "${SA_PASSWORD}" -C -d "${DATABASE}" \
    -i "${SCRIPTS_DIR}/01-schema.sql"
fi

echo "[stratix-init] Database initialization completed successfully."
