export type ActivityEventKey =
  | 'PROJECT_CREATED'
  | 'STAGE_COMPLETED'
  | 'TASK_ASSIGNED'
  | 'RISK_ADDED'
  | 'FILE_UPLOADED'
  | 'COMMENT_ADDED'
  | 'COMMENT_ADDED';

export type AuditEntity =
  | 'PROJECT'
  | 'STAGE'
  | 'TASK'
  | 'RISK'
  | 'USER'
  | 'MILESTONE' // legacy audit records only
  | 'COMMENT'
  | 'ATTACHMENT'
  | 'REPORT';

export type AuditAction =
  | 'CREATE'
  | 'UPDATE'
  | 'DELETE'
  | 'STATUS_CHANGE'
  | 'ASSIGNMENT_CHANGE'
  | 'PRIORITY_CHANGE'
  | 'MOVE'
  | 'COMMENT'
  | 'UPLOAD'
  | 'EXPORT';

export interface AuditEntry {
  id: number;
  entityType: AuditEntity;
  entityId: number;
  entityLabel: string;
  action: AuditAction;
  actor: string;
  actorRole?: string;
  userId?: number | null;
  projectId?: number | null;
  projectName?: string | null;
  field?: string;
  oldValue?: string;
  newValue?: string;
  oldValues?: string | null;
  newValues?: string | null;
  details: string;
  activityKey?: ActivityEventKey;
  at: string;
  ipAddress?: string | null;
  userAgent?: string | null;
}

export interface AuditLogInput {
  entityType: AuditEntity;
  entityId: number;
  entityLabel: string;
  action: AuditAction;
  details: string;
  activityKey?: ActivityEventKey;
  actor?: string;
  projectId?: number;
  projectName?: string;
  field?: string;
  oldValue?: string;
  newValue?: string;
}

export interface AuditLogQuery {
  entityType?: AuditEntity | 'ALL';
  entityId?: number | null;
  userId?: number | null;
  action?: AuditAction | 'ALL';
  projectId?: number | null;
  startDate?: string;
  endDate?: string;
  search?: string;
  page?: number;
  size?: number;
}

export interface AuditLogPage {
  content: AuditEntry[];
  totalElements: number;
  totalPages: number;
  page: number;
  size: number;
}

export interface AuditLogApiRow {
  id: number;
  userId: number | null;
  userName: string | null;
  action: AuditAction;
  entityType: AuditEntity;
  entityId: number;
  entityName: string;
  oldValues: string | null;
  newValues: string | null;
  description: string;
  ipAddress: string | null;
  userAgent: string | null;
  projectId: number | null;
  projectName: string | null;
  createdAt: string;
}
