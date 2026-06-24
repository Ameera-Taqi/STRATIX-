import { Component, computed, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TaskCard } from '../../../core/data/mock-data';
import { TasksStore } from '../../../core/services/tasks.store';
import { EmployeesStore } from '../../../core/services/employees.store';
import { ProjectsStore } from '../../../core/services/projects.store';
import { priorityClass } from '../../utils/status.util';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-task-board',
  standalone: true,
  imports: [RouterLink, TranslatePipe, FormsModule],
  templateUrl: './task-board.component.html',
})
export class TaskBoardComponent {
  readonly projectId = input<number | null>(null);
  readonly embedded = input(false);

  private readonly tasksStore = inject(TasksStore);
  private readonly projectsStore = inject(ProjectsStore);
  private readonly employeesStore = inject(EmployeesStore);

  readonly employees = this.employeesStore.employees;

  readonly columns = [
    { key: 'TODO' as const, labelKey: 'task.todo' },
    { key: 'IN_PROGRESS' as const, labelKey: 'task.inProgress' },
    { key: 'REVIEW' as const, labelKey: 'task.review' },
    { key: 'DONE' as const, labelKey: 'task.done' },
  ];

  readonly priorityClass = priorityClass;
  readonly showAddForm = signal(false);
  readonly taskError = signal<string | null>(null);
  readonly draggingTaskId = signal<number | null>(null);
  readonly dropTarget = signal<TaskCard['status'] | null>(null);

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

  tasksIn(status: TaskCard['status']) {
    return this.tasks().filter((t) => t.status === status);
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

  onDragOver(event: DragEvent, status: TaskCard['status']): void {
    event.preventDefault();
    this.dropTarget.set(status);
  }

  onDrop(status: TaskCard['status']): void {
    const id = this.draggingTaskId();
    if (id != null) this.tasksStore.moveTask(id, status);
    this.draggingTaskId.set(null);
    this.dropTarget.set(null);
  }
}
