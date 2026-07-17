/** Map API/UI enum codes to i18n translation keys. */

export function projectStatusLabelKey(status: string): string {
  const s = normalize(status);
  const map: Record<string, string> = {
    ACTIVE: 'status.active',
    REVIEW: 'status.review',
    DELAYED: 'status.delayed',
    PLANNED: 'status.planned',
    ON_HOLD: 'status.onHold',
    ONHOLD: 'status.onHold',
    COMPLETED: 'status.completed',
    DONE: 'status.done',
    CANCELLED: 'status.cancelled',
    CANCELED: 'status.cancelled',
  };
  return map[s] ?? status;
}

export function taskStatusLabelKey(status: string): string {
  const s = normalize(status);
  const map: Record<string, string> = {
    TODO: 'status.todo',
    IN_PROGRESS: 'status.inProgress',
    INPROGRESS: 'status.inProgress',
    REVIEW: 'status.review',
    DONE: 'status.done',
  };
  return map[s] ?? projectStatusLabelKey(status);
}

export function priorityLabelKey(priority: string): string {
  const s = normalize(priority);
  const map: Record<string, string> = {
    LOW: 'priority.low',
    MEDIUM: 'priority.medium',
    HIGH: 'priority.high',
    URGENT: 'priority.urgent',
    CRITICAL: 'priority.critical',
  };
  return map[s] ?? priority;
}

export function riskLevelLabelKey(level: string): string {
  return priorityLabelKey(level);
}

export function riskStatusLabelKey(status: string): string {
  const s = normalize(status);
  const map: Record<string, string> = {
    OPEN: 'risks.status.open',
    MITIGATING: 'risks.status.mitigating',
    CLOSED: 'risks.status.closed',
  };
  return map[s] ?? status;
}

export function riskImpactLabelKey(impact: string): string {
  return priorityLabelKey(impact);
}

export function riskProbabilityLabelKey(probability: string): string {
  return priorityLabelKey(probability);
}

function normalize(value: string): string {
  return value.trim().toUpperCase().replace(/[\s-]+/g, '_');
}
