import { ActivityEventKey, AuditEntry } from '../../core/models/audit.model';

export interface ActivityEventMeta {
  key: ActivityEventKey | 'OTHER';
  icon: string;
  dotClass: string;
  iconClass: string;
}

const META: Record<ActivityEventKey, Omit<ActivityEventMeta, 'key'>> = {
  PROJECT_CREATED: {
    icon: '📁',
    dotClass: 'bg-blue-500 ring-blue-100 dark:ring-blue-900/40',
    iconClass: 'bg-blue-50 text-blue-600 dark:bg-blue-900/30 dark:text-blue-300',
  },
  STAGE_COMPLETED: {
    icon: '✓',
    dotClass: 'bg-emerald-500 ring-emerald-100 dark:ring-emerald-900/40',
    iconClass: 'bg-emerald-50 text-emerald-600 dark:bg-emerald-900/30 dark:text-emerald-300',
  },
  TASK_ASSIGNED: {
    icon: '👤',
    dotClass: 'bg-violet-500 ring-violet-100 dark:ring-violet-900/40',
    iconClass: 'bg-violet-50 text-violet-600 dark:bg-violet-900/30 dark:text-violet-300',
  },
  RISK_ADDED: {
    icon: '⚠',
    dotClass: 'bg-amber-500 ring-amber-100 dark:ring-amber-900/40',
    iconClass: 'bg-amber-50 text-amber-600 dark:bg-amber-900/30 dark:text-amber-300',
  },
  FILE_UPLOADED: {
    icon: '📎',
    dotClass: 'bg-sky-500 ring-sky-100 dark:ring-sky-900/40',
    iconClass: 'bg-sky-50 text-sky-600 dark:bg-sky-900/30 dark:text-sky-300',
  },
  COMMENT_ADDED: {
    icon: '💬',
    dotClass: 'bg-pink-500 ring-pink-100 dark:ring-pink-900/40',
    iconClass: 'bg-pink-50 text-pink-600 dark:bg-pink-900/30 dark:text-pink-300',
  },
};

function isStageCompleted(entry: AuditEntry): boolean {
  if (entry.entityType !== 'STAGE') return false;
  const done = entry.newValue?.toLowerCase() === 'done';
  return (
    entry.activityKey === 'STAGE_COMPLETED' ||
    (done && (entry.action === 'STATUS_CHANGE' || entry.action === 'UPDATE')) ||
    entry.details.toLowerCase().includes('stage completed')
  );
}

export function resolveActivityKey(entry: AuditEntry): ActivityEventKey | 'OTHER' {
  if (entry.activityKey) return entry.activityKey;

  if (entry.entityType === 'PROJECT' && entry.action === 'CREATE') return 'PROJECT_CREATED';
  if (isStageCompleted(entry)) return 'STAGE_COMPLETED';
  if (entry.entityType === 'TASK' && entry.field === 'assignee' && entry.action === 'UPDATE') {
    return 'TASK_ASSIGNED';
  }
  if (entry.entityType === 'TASK' && entry.action === 'CREATE' && entry.details.toLowerCase().includes('assigned')) {
    return 'TASK_ASSIGNED';
  }
  if (entry.entityType === 'RISK' && entry.action === 'CREATE') return 'RISK_ADDED';
  if (entry.entityType === 'ATTACHMENT' && entry.action === 'UPLOAD') return 'FILE_UPLOADED';
  if (entry.entityType === 'COMMENT' && entry.action === 'COMMENT') return 'COMMENT_ADDED';

  return 'OTHER';
}

export function activityEventMeta(entry: AuditEntry): ActivityEventMeta {
  const key = resolveActivityKey(entry);
  if (key === 'OTHER') {
    return {
      key,
      icon: '•',
      dotClass: 'bg-slate-400 ring-slate-100 dark:ring-slate-700',
      iconClass: 'bg-slate-100 text-slate-600 dark:bg-slate-800 dark:text-slate-300',
    };
  }
  return { key, ...META[key] };
}
