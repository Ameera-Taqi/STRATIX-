import { SystemModuleCode } from './stratix-modules';

export type QuickCreateAction =
  | 'project'
  | 'feature'
  | 'task'
  | 'risk'
  | 'user'
  | 'report';

export interface QuickCreateItem {
  id: QuickCreateAction;
  labelKey: string;
  /** Module write permission required to show this action. */
  module: SystemModuleCode;
  icon: string;
}

/** Ordered Quick Create options — filtered at runtime by RoleAccessService.canWrite. */
export const QUICK_CREATE_ITEMS: readonly QuickCreateItem[] = [
  { id: 'project', labelKey: 'quickCreate.project', module: 'PROJECTS', icon: 'folder' },
  { id: 'feature', labelKey: 'quickCreate.feature', module: 'STAGES', icon: 'clipboard' },
  { id: 'task', labelKey: 'quickCreate.task', module: 'TASKS', icon: 'check' },
  { id: 'risk', labelKey: 'quickCreate.risk', module: 'RISKS', icon: 'warning' },
  { id: 'user', labelKey: 'quickCreate.user', module: 'EMPLOYEES', icon: 'user' },
  { id: 'report', labelKey: 'quickCreate.report', module: 'REPORTS', icon: 'file' },
] as const;
