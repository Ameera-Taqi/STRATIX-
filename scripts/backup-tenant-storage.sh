#!/usr/bin/env bash
# Backup Stratix tenant file storage (Docker named volume stratix_api_data).
# Usage: ./scripts/backup-tenant-storage.sh [output-dir]
set -euo pipefail

VOLUME="${STRATIX_STORAGE_VOLUME:-stratix_api_data}"
OUT_DIR="${1:-./backups/storage}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
ARCHIVE="${OUT_DIR}/stratix-files-${STAMP}.tar.gz"

mkdir -p "${OUT_DIR}"

if ! docker volume inspect "${VOLUME}" >/dev/null 2>&1; then
  echo "Volume '${VOLUME}' not found. Start the stack once (docker compose up -d) or set STRATIX_STORAGE_VOLUME." >&2
  exit 1
fi

echo "Backing up Docker volume '${VOLUME}' → ${ARCHIVE}"
docker run --rm \
  -v "${VOLUME}:/data:ro" \
  -v "$(cd "${OUT_DIR}" && pwd):/backups" \
  alpine:3.20 \
  tar czf "/backups/$(basename "${ARCHIVE}")" -C /data .

echo "Done. Restore example:"
echo "  docker run --rm -v ${VOLUME}:/data -v \$(pwd)/backups:/backups alpine:3.20 tar xzf /backups/$(basename "${ARCHIVE}") -C /data"
