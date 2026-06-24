import { AuditEntry, AuditLogApiRow } from '../../core/models/audit.model';

export function mapAuditLogRow(row: AuditLogApiRow): AuditEntry {
  const parsed = parseChangeFields(row.oldValues, row.newValues);
  return {
    id: row.id,
    entityType: row.entityType,
    entityId: row.entityId,
    entityLabel: row.entityName,
    action: row.action,
    actor: row.userName ?? 'System',
    userId: row.userId,
    projectId: row.projectId,
    projectName: row.projectName,
    field: parsed.field,
    oldValue: parsed.oldValue,
    newValue: parsed.newValue,
    oldValues: row.oldValues,
    newValues: row.newValues,
    details: row.description,
    at: row.createdAt,
    ipAddress: row.ipAddress,
    userAgent: row.userAgent,
  };
}

function parseChangeFields(
  oldValues: string | null,
  newValues: string | null,
): { field?: string; oldValue?: string; newValue?: string } {
  try {
    const oldObj = oldValues ? (JSON.parse(oldValues) as Record<string, unknown>) : null;
    const newObj = newValues ? (JSON.parse(newValues) as Record<string, unknown>) : null;
    if (!oldObj && !newObj) return {};
    const keys = new Set([...Object.keys(oldObj ?? {}), ...Object.keys(newObj ?? {})]);
    for (const key of keys) {
      const oldVal = oldObj?.[key];
      const newVal = newObj?.[key];
      if (String(oldVal ?? '') !== String(newVal ?? '')) {
        return {
          field: key,
          oldValue: oldVal != null ? String(oldVal) : undefined,
          newValue: newVal != null ? String(newVal) : undefined,
        };
      }
    }
  } catch {
    /* ignore malformed JSON */
  }
  return {};
}
