import { ProjectRow } from '../models/project.model';
import { StageRow } from '../models/stage.model';

export function asDateString(value: string | null | undefined): string {
  if (!value) return '';
  return String(value).slice(0, 10);
}

export function normalizeProjectRow(project: ProjectRow): ProjectRow {
  const manager = project.manager?.trim() || project.owner?.trim() || '';
  return {
    ...project,
    id: Number(project.id),
    manager,
    managerId: project.managerId == null ? null : Number(project.managerId),
    owner: project.owner?.trim() || manager,
    startDate: asDateString(project.startDate),
    endDate: asDateString(project.endDate),
    deadline: asDateString(project.deadline ?? project.endDate),
    priority: (project.priority ?? 'MEDIUM') as ProjectRow['priority'],
    progress: Number(project.progress) || 0,
  };
}

export function normalizeStageRow(stage: StageRow): StageRow {
  return {
    ...stage,
    startDate: asDateString(stage.startDate),
    endDate: asDateString(stage.endDate),
  };
}
