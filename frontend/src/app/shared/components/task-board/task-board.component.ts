import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TaskCard } from '../../../core/data/mock-data';
import { TasksStore } from '../../../core/services/tasks.store';
import { EmployeesStore } from '../../../core/services/employees.store';
import { ProjectsStore } from '../../../core/services/projects.store';
import { LanguageService } from '../../../core/i18n/language.service';
import { CurrentUserService } from '../../../core/services/current-user.service';
import { priorityClass } from '../../utils/status.util';
import { priorityLabelKey } from '../../utils/enum-labels';
import {
  analyzeTaskTransition,
  TaskStatus,
  TransitionAnalysis,
} from '../../utils/task-transition.util';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { UiIconComponent } from '../ui-icon/ui-icon.component';
import { UserRole } from '../../../core/models/user.model';

export type BoardColumnStatus = TaskCard['status'];

export interface BoardColumn {
  id: string;
  /** i18n key for built-in columns; custom columns use `label` instead. */
  labelKey?: string;
  label?: string;
  status: BoardColumnStatus;
  dotClass: string;
  builtIn: boolean;
}

const DEFAULT_COLUMNS: BoardColumn[] = [
  { id: 'TODO', labelKey: 'work.workflow.todo', status: 'TODO', dotClass: 'bg-slate-400', builtIn: true },
  {
    id: 'IN_PROGRESS',
    labelKey: 'work.workflow.inProgress',
    status: 'IN_PROGRESS',
    dotClass: 'bg-sky-500',
    builtIn: true,
  },
  { id: 'REVIEW', labelKey: 'work.workflow.review', status: 'REVIEW', dotClass: 'bg-violet-500', builtIn: true },
  { id: 'BLOCKED', labelKey: 'work.workflow.blocked', status: 'BLOCKED', dotClass: 'bg-rose-500', builtIn: true },
  { id: 'DONE', labelKey: 'work.workflow.done', status: 'DONE', dotClass: 'bg-emerald-500', builtIn: true },
];

const DOT_OPTIONS = ['bg-amber-500', 'bg-violet-500', 'bg-rose-500', 'bg-teal-500', 'bg-orange-500', 'bg-indigo-500'];

function storageKey(projectId: number): string {
  return `stratix.board.columns.${projectId}`;
}

function hiddenStorageKey(projectId: number): string {
  return `stratix.board.hidden.${projectId}`;
}

function loadCustomColumns(projectId: number): BoardColumn[] {
  try {
    const raw = localStorage.getItem(storageKey(projectId));
    if (!raw) return [];
    const parsed = JSON.parse(raw) as BoardColumn[];
    return Array.isArray(parsed) ? parsed.filter((c) => c && !c.builtIn && c.id && c.status) : [];
  } catch {
    return [];
  }
}

function saveCustomColumns(projectId: number, columns: BoardColumn[]): void {
  localStorage.setItem(storageKey(projectId), JSON.stringify(columns.filter((c) => !c.builtIn)));
}

function loadHiddenColumnIds(projectId: number): string[] {
  try {
    const raw = localStorage.getItem(hiddenStorageKey(projectId));
    if (!raw) return [];
    const parsed = JSON.parse(raw) as string[];
    return Array.isArray(parsed) ? parsed.filter((id) => typeof id === 'string') : [];
  } catch {
    return [];
  }
}

function saveHiddenColumnIds(projectId: number, ids: string[]): void {
  localStorage.setItem(hiddenStorageKey(projectId), JSON.stringify(ids));
}

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [RouterLink, TranslatePipe, FormsModule, UiIconComponent],
  templateUrl: './task-board.component.html',
})
export class TaskBoardComponent {
  readonly projectId = input<number | null>(null);
  readonly embedded = input(false);

  private readonly tasksStore = inject(TasksStore);
  private readonly projectsStore = inject(ProjectsStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly lang = inject(LanguageService);
  private readonly currentUser = inject(CurrentUserService);

  readonly employees = this.employeesStore.employees;

  readonly customColumns = signal<BoardColumn[]>([]);
  readonly hiddenColumnIds = signal<string[]>([]);
  readonly showAddColumn = signal(false);
  readonly columnError = signal<string | null>(null);

  readonly boardColumns = computed(() => {
    const hidden = new Set(this.hiddenColumnIds());
    const builtInStatuses = new Set(DEFAULT_COLUMNS.map((c) => c.status));
    const visibleBuiltIn = DEFAULT_COLUMNS.filter((c) => !hidden.has(c.id));
    const custom = this.customColumns().filter(
      (c) => !builtInStatuses.has(c.status) && !hidden.has(c.id),
    );
    return [...visibleBuiltIn, ...custom];
  });

  readonly showEmptyState = computed(
    () => this.tasks().length === 0 && this.boardColumns().length === 0 && !this.showAddColumn(),
  );

  readonly statusOptions: { value: BoardColumnStatus; labelKey: string }[] = [
    { value: 'TODO', labelKey: 'work.workflow.todo' },
    { value: 'IN_PROGRESS', labelKey: 'work.workflow.inProgress' },
    { value: 'REVIEW', labelKey: 'work.workflow.review' },
    { value: 'BLOCKED', labelKey: 'work.workflow.blocked' },
    { value: 'DONE', labelKey: 'work.workflow.done' },
  ];

  readonly priorities: TaskCard['priority'][] = ['LOW', 'MEDIUM', 'HIGH', 'URGENT'];

  columnForm = {
    name: '',
    status: 'REVIEW' as BoardColumnStatus,
  };

  readonly priorityClass = priorityClass;
  readonly showAddForm = signal(false);
  readonly taskError = signal<string | null>(null);
  readonly draggingTaskId = signal<number | null>(null);
  readonly dropTarget = signal<string | null>(null);

  /** Friendly guidance when a drop targets an illegal column. */
  readonly transitionHint = signal<{
    taskId: number;
    title: string;
    messageKey: string;
    suggestedStatus?: TaskStatus;
    suggestedActionKey?: string;
  } | null>(null);

  /** Collect blocked / reopen / request-changes reason before calling the API. */
  readonly reasonModal = signal<{
    kind: 'blocked' | 'reopen' | 'requestChanges';
    taskId: number;
    title: string;
    targetStatus: TaskStatus;
  } | null>(null);
  reasonText = '';
  readonly reasonError = signal<string | null>(null);

  private static readonly REVIEWER_ROLES: UserRole[] = [
    'SUPER_ADMIN',
    'ORG_ADMIN',
    'ADMIN',
    'PROJECT_MANAGER',
    'TEAM_LEADER',
  ];

  readonly canReviewTasks = computed(() => {
    const role = this.currentUser.profile()?.roleCode;
    return !!role && TaskBoardComponent.REVIEWER_ROLES.includes(role);
  });

  readonly awaitingReview = computed(() =>
    this.tasks()
      .filter((t) => t.status === 'REVIEW')
      .slice()
      .sort((a, b) => {
        const at = a.submittedForReviewAt ?? '';
        const bt = b.submittedForReviewAt ?? '';
        return at.localeCompare(bt);
      }),
  );

  readonly tasks = computed(() => {
    this.tasksStore.tasks();
    const pid = this.projectId();
    return pid != null ? this.tasksStore.getByProject(pid) : this.tasksStore.getAll();
  });

  readonly project = computed(() => {
    const pid = this.projectId();
    return pid != null ? this.projectsStore.getById(pid) : null;
  });

  readonly stages = computed(() => {
    const pid = this.projectId();
    return pid != null ? this.projectsStore.getStages(pid) : [];
  });

  taskForm = {
    title: '',
    description: '',
    assigneeId: null as number | null,
    priority: 'MEDIUM' as TaskCard['priority'],
    estimatedHours: 1,
    startDate: '',
    dueDate: '',
    status: 'TODO' as TaskCard['status'],
    stageId: null as number | null,
  };

  constructor() {
    effect(() => {
      const pid = this.projectId();
      this.customColumns.set(pid != null ? loadCustomColumns(pid) : []);
      this.hiddenColumnIds.set(pid != null ? loadHiddenColumnIds(pid) : []);
      this.showAddColumn.set(false);
      this.columnError.set(null);
    });
  }

  tasksInColumn(column: BoardColumn): TaskCard[] {
    return this.tasks().filter((t) => t.status === column.status);
  }

  priorityLabel(priority: TaskCard['priority']): string {
    return this.lang.t(priorityLabelKey(priority));
  }

  formatDue(date: string | undefined | null): string {
    if (!date) return '';
    const d = new Date(`${String(date).slice(0, 10)}T12:00:00`);
    if (Number.isNaN(d.getTime())) return String(date);
    const locale = this.lang.lang() === 'ar' ? 'ar' : 'en-US';
    const formatted = d.toLocaleDateString(locale, { month: 'short', day: 'numeric' });
    return `${this.lang.t('common.due')} ${formatted}`;
  }

  hoursLabel(hours: number | undefined | null): string {
    const n = Number(hours);
    if (!Number.isFinite(n) || n <= 0) return '';
    return `${n % 1 === 0 ? n : n.toFixed(1)}h`;
  }

  submittedByLabel(task: TaskCard): string {
    return task.submittedForReviewBy?.trim() || task.assignee?.trim() || '—';
  }

  submittedAgo(task: TaskCard): string {
    const raw = task.submittedForReviewAt;
    if (!raw) return this.lang.t('tasks.review.justNow');
    const at = new Date(raw).getTime();
    if (Number.isNaN(at)) return this.lang.t('tasks.review.justNow');
    const mins = Math.max(0, Math.round((Date.now() - at) / 60000));
    if (mins < 1) return this.lang.t('tasks.review.justNow');
    if (mins < 60) return this.lang.t('tasks.review.minutesAgo').replace('{{n}}', String(mins));
    const hours = Math.round(mins / 60);
    if (hours < 48) return this.lang.t('tasks.review.hoursAgo').replace('{{n}}', String(hours));
    const days = Math.round(hours / 24);
    return this.lang.t('tasks.review.daysAgo').replace('{{n}}', String(days));
  }

  submitForReview(taskId: number, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    this.requestStatusChange(taskId, 'REVIEW');
  }

  approveReview(taskId: number, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    this.requestStatusChange(taskId, 'DONE');
  }

  requestChanges(taskId: number, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    this.requestStatusChange(taskId, 'IN_PROGRESS');
  }

  openAddColumn(): void {
    this.columnError.set(null);
    this.columnForm = { name: '', status: 'REVIEW' };
    this.showAddColumn.set(true);
  }

  cancelAddColumn(): void {
    this.showAddColumn.set(false);
    this.columnError.set(null);
  }

  saveColumn(): void {
    const name = this.columnForm.name.trim();
    if (!name) {
      this.columnError.set('board.columnErrorName');
      return;
    }

    if (this.customColumns().some((c) => (c.label ?? '').toLowerCase() === name.toLowerCase())) {
      this.columnError.set('board.columnErrorDuplicate');
      return;
    }

    const pid = this.projectId();
    // Re-show a hidden built-in column mapped to the same workflow status.
    const matchingBuiltIn = DEFAULT_COLUMNS.find((c) => c.status === this.columnForm.status);
    if (matchingBuiltIn && this.hiddenColumnIds().includes(matchingBuiltIn.id)) {
      const hidden = this.hiddenColumnIds().filter((id) => id !== matchingBuiltIn.id);
      this.hiddenColumnIds.set(hidden);
      if (pid != null) saveHiddenColumnIds(pid, hidden);
      this.showAddColumn.set(false);
      this.columnError.set(null);
      return;
    }

    const next: BoardColumn = {
      id: `custom-${Date.now()}`,
      label: name,
      status: this.columnForm.status,
      dotClass: DOT_OPTIONS[this.customColumns().length % DOT_OPTIONS.length],
      builtIn: false,
    };
    const updated = [...this.customColumns(), next];
    this.customColumns.set(updated);
    if (pid != null) saveCustomColumns(pid, updated);
    this.showAddColumn.set(false);
    this.columnError.set(null);
  }

  removeColumn(column: BoardColumn, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    if (this.boardColumns().length <= 1) {
      window.alert(this.lang.t('board.columnErrorLast'));
      return;
    }

    const taskCount = this.tasksInColumn(column).length;
    const label = column.labelKey ? this.lang.t(column.labelKey) : (column.label ?? column.id);
    const message =
      taskCount > 0
        ? this.lang
            .t('board.removeColumnConfirmWithTasks')
            .replace('{{column}}', label)
            .replace('{{count}}', String(taskCount))
        : this.lang.t('board.removeColumnConfirm').replace('{{column}}', label);
    if (!window.confirm(message)) return;

    const pid = this.projectId();
    if (column.builtIn) {
      const hidden = [...new Set([...this.hiddenColumnIds(), column.id])];
      this.hiddenColumnIds.set(hidden);
      if (pid != null) saveHiddenColumnIds(pid, hidden);
      return;
    }

    const updated = this.customColumns().filter((c) => c.id !== column.id);
    this.customColumns.set(updated);
    if (pid != null) saveCustomColumns(pid, updated);
  }

  openAddTask(): void {
    const p = this.project();
    const defaultAssignee = this.employees().find((e) => e.name === p?.manager);
    this.taskError.set(null);
    this.taskForm = {
      title: '',
      description: '',
      assigneeId: defaultAssignee?.id ?? null,
      priority: 'MEDIUM',
      estimatedHours: 1,
      startDate: new Date().toISOString().slice(0, 10),
      dueDate: p?.endDate ?? '',
      status: 'TODO',
      stageId: this.stages().length === 1 ? this.stages()[0].id : null,
    };
    this.showAddForm.set(true);
  }

  saveTask(): void {
    const pid = this.projectId();
    if (pid == null) return;
    if (!this.taskForm.title.trim()) {
      this.taskError.set('tasks.errorTitle');
      return;
    }
    if (this.stages().length === 0) {
      this.taskError.set('tasks.errorFeatureRequired');
      return;
    }
    if (this.taskForm.stageId == null) {
      this.taskError.set('tasks.errorFeature');
      return;
    }
    if (!(Number(this.taskForm.estimatedHours) > 0)) {
      this.taskError.set('tasks.errorHours');
      return;
    }
    const assignee = this.employees().find((e) => e.id === this.taskForm.assigneeId);
    this.tasksStore.addTask({
      projectId: pid,
      title: this.taskForm.title,
      description: this.taskForm.description,
      assignee: assignee?.name ?? '',
      assigneeId: this.taskForm.assigneeId,
      priority: this.taskForm.priority,
      estimatedHours: Number(this.taskForm.estimatedHours),
      startDate: this.taskForm.startDate,
      dueDate: this.taskForm.dueDate,
      status: this.taskForm.status,
      stageId: this.taskForm.stageId,
    });
    this.showAddForm.set(false);
  }

  onDragStart(taskId: number): void {
    this.draggingTaskId.set(taskId);
  }

  onDragEnd(): void {
    this.draggingTaskId.set(null);
    this.dropTarget.set(null);
  }

  onDragOver(event: DragEvent, columnId: string): void {
    event.preventDefault();
    this.dropTarget.set(columnId);
  }

  onDrop(column: BoardColumn): void {
    const id = this.draggingTaskId();
    this.draggingTaskId.set(null);
    this.dropTarget.set(null);
    if (id == null) return;
    this.requestStatusChange(id, column.status);
  }

  /** Card CTA: unblock → In Progress (opens reopen modal only when leaving DONE). */
  moveToInProgress(taskId: number, event?: Event): void {
    event?.preventDefault();
    event?.stopPropagation();
    this.requestStatusChange(taskId, 'IN_PROGRESS');
  }

  dismissTransitionHint(): void {
    this.transitionHint.set(null);
  }

  applySuggestedTransition(): void {
    const hint = this.transitionHint();
    if (!hint?.suggestedStatus) return;
    const taskId = hint.taskId;
    const status = hint.suggestedStatus;
    this.transitionHint.set(null);
    this.requestStatusChange(taskId, status);
  }

  cancelReasonModal(): void {
    this.reasonModal.set(null);
    this.reasonText = '';
    this.reasonError.set(null);
  }

  confirmReasonModal(): void {
    const modal = this.reasonModal();
    if (!modal) return;
    const reason = this.reasonText.trim();
    if (!reason) {
      const errKey =
        modal.kind === 'blocked'
          ? 'tasks.transition.blockedReasonRequired'
          : modal.kind === 'requestChanges'
            ? 'tasks.review.feedbackRequired'
            : 'tasks.transition.reopenReasonRequired';
      this.reasonError.set(errKey);
      return;
    }
    if (modal.kind === 'blocked') {
      this.tasksStore.moveTask(modal.taskId, modal.targetStatus, { blockedReason: reason });
    } else if (modal.kind === 'requestChanges') {
      this.tasksStore.moveTask(modal.taskId, modal.targetStatus, { reviewReason: reason });
    } else {
      this.tasksStore.moveTask(modal.taskId, modal.targetStatus, { reopenReason: reason });
    }
    this.cancelReasonModal();
  }

  private requestStatusChange(taskId: number, targetStatus: TaskStatus): void {
    const task = this.tasksStore.getById(taskId);
    if (!task || task.status === targetStatus) return;

    this.transitionHint.set(null);
    const plan: TransitionAnalysis = analyzeTaskTransition(task.status, targetStatus);

    if (!plan.allowed) {
      this.transitionHint.set({
        taskId: task.id,
        title: task.title,
        messageKey: plan.messageKey ?? 'tasks.transition.notAllowed',
        suggestedStatus: plan.suggestedStatus,
        suggestedActionKey: plan.suggestedActionKey,
      });
      return;
    }

    if (plan.requirement === 'blockedReason') {
      this.reasonText = '';
      this.reasonError.set(null);
      this.reasonModal.set({
        kind: 'blocked',
        taskId: task.id,
        title: task.title,
        targetStatus: 'BLOCKED',
      });
      return;
    }

    if (plan.requirement === 'reopenReason') {
      this.reasonText = '';
      this.reasonError.set(null);
      this.reasonModal.set({
        kind: 'reopen',
        taskId: task.id,
        title: task.title,
        targetStatus,
      });
      return;
    }

    if (plan.requirement === 'requestChanges') {
      this.reasonText = '';
      this.reasonError.set(null);
      this.reasonModal.set({
        kind: 'requestChanges',
        taskId: task.id,
        title: task.title,
        targetStatus: 'IN_PROGRESS',
      });
      return;
    }

    this.tasksStore.moveTask(taskId, targetStatus);
  }
}
