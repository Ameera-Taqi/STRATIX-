import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TaskCard } from '../../../core/data/mock-data';
import { TasksStore } from '../../../core/services/tasks.store';
import { EmployeesStore } from '../../../core/services/employees.store';
import { ProjectsStore } from '../../../core/services/projects.store';
import { priorityClass } from '../../utils/status.util';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { UiIconComponent } from '../ui-icon/ui-icon.component';

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
  { id: 'DONE', labelKey: 'work.workflow.done', status: 'DONE', dotClass: 'bg-emerald-500', builtIn: true },
];

const DOT_OPTIONS = ['bg-amber-500', 'bg-violet-500', 'bg-rose-500', 'bg-teal-500', 'bg-orange-500', 'bg-indigo-500'];

function storageKey(projectId: number): string {
  return `stratix.board.columns.${projectId}`;
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

  readonly employees = this.employeesStore.employees;

  readonly customColumns = signal<BoardColumn[]>([]);
  readonly showAddColumn = signal(false);
  readonly columnError = signal<string | null>(null);

  readonly boardColumns = computed(() => [...DEFAULT_COLUMNS, ...this.customColumns()]);

  readonly showEmptyState = computed(
    () => this.tasks().length === 0 && this.customColumns().length === 0 && !this.showAddColumn(),
  );

  readonly statusOptions: { value: BoardColumnStatus; labelKey: string }[] = [
    { value: 'TODO', labelKey: 'work.workflow.todo' },
    { value: 'IN_PROGRESS', labelKey: 'work.workflow.inProgress' },
    { value: 'REVIEW', labelKey: 'work.workflow.review' },
    { value: 'DONE', labelKey: 'work.workflow.done' },
  ];

  columnForm = {
    name: '',
    status: 'REVIEW' as BoardColumnStatus,
  };

  readonly priorityClass = priorityClass;
  readonly showAddForm = signal(false);
  readonly taskError = signal<string | null>(null);
  readonly draggingTaskId = signal<number | null>(null);
  readonly dropTarget = signal<string | null>(null);

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
    assigneeId: null as number | null,
    priority: 'MEDIUM' as TaskCard['priority'],
    dueDate: '',
    status: 'TODO' as TaskCard['status'],
    stageId: null as number | null,
  };

  constructor() {
    effect(() => {
      const pid = this.projectId();
      this.customColumns.set(pid != null ? loadCustomColumns(pid) : []);
      this.showAddColumn.set(false);
      this.columnError.set(null);
    });
  }

  tasksInColumn(column: BoardColumn): TaskCard[] {
    const columns = this.boardColumns();
    const hasReviewColumn = columns.some((c) => c.status === 'REVIEW');

    return this.tasks().filter((t) => {
      if (column.status === 'TODO') return t.status === 'TODO';
      if (column.status === 'DONE') return t.status === 'DONE';
      if (column.status === 'REVIEW') return t.status === 'REVIEW';
      // In Progress: include REVIEW only when there is no dedicated Review column
      if (column.status === 'IN_PROGRESS') {
        return t.status === 'IN_PROGRESS' || (!hasReviewColumn && t.status === 'REVIEW');
      }
      return t.status === column.status;
    });
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

    const status = this.columnForm.status;
    if (status === 'REVIEW' && this.boardColumns().some((c) => c.status === 'REVIEW')) {
      this.columnError.set('board.columnErrorReviewExists');
      return;
    }
    if (this.customColumns().some((c) => (c.label ?? '').toLowerCase() === name.toLowerCase())) {
      this.columnError.set('board.columnErrorDuplicate');
      return;
    }

    const pid = this.projectId();
    const next: BoardColumn = {
      id: `custom-${Date.now()}`,
      label: name,
      status,
      dotClass: DOT_OPTIONS[this.customColumns().length % DOT_OPTIONS.length],
      builtIn: false,
    };
    const updated = [...this.customColumns(), next];
    this.customColumns.set(updated);
    if (pid != null) saveCustomColumns(pid, updated);
    this.showAddColumn.set(false);
    this.columnError.set(null);
  }

  removeColumn(column: BoardColumn): void {
    if (column.builtIn) return;
    const pid = this.projectId();
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
      assigneeId: defaultAssignee?.id ?? this.employees()[0]?.id ?? null,
      priority: 'MEDIUM',
      dueDate: p?.endDate ?? new Date().toISOString().slice(0, 10),
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
    if (this.taskForm.assigneeId == null) {
      this.taskError.set('tasks.errorAssignee');
      return;
    }
    const assignee = this.employees().find((e) => e.id === this.taskForm.assigneeId);
    this.tasksStore.addTask({
      projectId: pid,
      title: this.taskForm.title,
      assignee: assignee?.name ?? '',
      assigneeId: this.taskForm.assigneeId,
      priority: this.taskForm.priority,
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
    if (id != null) this.tasksStore.moveTask(id, column.status);
    this.draggingTaskId.set(null);
    this.dropTarget.set(null);
  }
}
