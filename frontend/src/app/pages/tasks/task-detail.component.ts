import { Component, OnInit, computed, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TasksStore } from '../../core/services/tasks.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { RoleAccessService } from '../../core/services/role-access.service';
import { CommentsPanelComponent } from '../../shared/components/comments-panel/comments-panel.component';
import { AttachmentsPanelComponent } from '../../shared/components/attachments-panel/attachments-panel.component';
import { ActivityFeedComponent } from '../../shared/components/activity-feed/activity-feed.component';
import {
  BreadcrumbItem,
  BreadcrumbsComponent,
} from '../../shared/components/breadcrumbs/breadcrumbs.component';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { priorityClass } from '../../shared/utils/status.util';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { UserRole } from '../../core/models/user.model';

/** Mirrors API AuthPolicies.TeamLeaders — who may change assignee. */
const ASSIGN_ROLES: readonly UserRole[] = [
  'SUPER_ADMIN',
  'ORG_ADMIN',
  'ADMIN',
  'PROJECT_MANAGER',
  'TEAM_LEADER',
];

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [
    TopbarComponent,
    RouterLink,
    TranslatePipe,
    FormsModule,
    BreadcrumbsComponent,
    CommentsPanelComponent,
    AttachmentsPanelComponent,
    ActivityFeedComponent,
    UiIconComponent,
  ],
  templateUrl: './task-detail.component.html',
})
export class TaskDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly tasksStore = inject(TasksStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly roleAccess = inject(RoleAccessService);

  readonly taskId = Number(this.route.snapshot.paramMap.get('id'));
  readonly draftAssigneeId = signal<number | null>(null);
  private readonly draftReady = signal(false);
  readonly savingAssignee = signal(false);
  readonly assigneeMessage = signal<'saved' | 'failed' | null>(null);

  readonly task = computed(() => {
    this.tasksStore.tasks();
    return this.tasksStore.getById(this.taskId) ?? null;
  });

  readonly employees = this.employeesStore.employees;

  readonly canAssign = computed(() => {
    const role = this.roleAccess.role();
    return !!role && ASSIGN_ROLES.includes(role);
  });

  readonly assigneeDirty = computed(() => {
    const task = this.task();
    if (!task || !this.draftReady()) return false;
    return (this.draftAssigneeId() ?? null) !== (task.assigneeId ?? null);
  });

  readonly breadcrumbs = computed((): BreadcrumbItem[] => {
    const t = this.task();
    if (!t) {
      return [{ labelKey: 'nav.projects', link: '/projects' }];
    }
    const crumbs: BreadcrumbItem[] = [
      { labelKey: 'nav.projects', link: '/projects' },
      { label: t.projectName, link: ['/projects', t.projectId] },
    ];
    if (t.stageId != null && t.stageName) {
      crumbs.push({
        label: t.stageName,
        link: ['/projects', t.projectId],
        queryParams: { tab: 'features', featureId: t.stageId },
      });
    }
    crumbs.push({ label: t.title });
    return crumbs;
  });

  /** Prefer project Board; fall back to projects list when the task is missing. */
  readonly backLink = computed((): (string | number)[] => {
    const t = this.task();
    return t ? ['/projects', t.projectId] : ['/projects'];
  });

  readonly backQuery = computed((): Record<string, string> | null =>
    this.task() ? { tab: 'tasks' } : null,
  );

  readonly priorityClass = priorityClass;
  readonly subtasks: { name: string; progress: number; status: string }[] = [];

  constructor() {
    effect(() => {
      const t = this.task();
      if (!t) return;
      if (!this.draftReady()) {
        this.draftAssigneeId.set(t.assigneeId ?? null);
        this.draftReady.set(true);
        return;
      }
      if (this.savingAssignee() || this.assigneeDirty()) return;
      this.draftAssigneeId.set(t.assigneeId ?? null);
    });
  }

  projectLink(): (string | number)[] {
    const t = this.task();
    return t ? ['/projects', t.projectId] : ['/projects'];
  }

  onAssigneeDraftChange(assigneeId: number | null): void {
    if (!this.canAssign()) return;
    this.draftAssigneeId.set(assigneeId);
    this.assigneeMessage.set(null);
  }

  saveAssignee(): void {
    if (!this.canAssign() || !this.assigneeDirty() || this.savingAssignee()) return;
    const task = this.task();
    if (!task) return;

    const assigneeId = this.draftAssigneeId();
    const employee =
      assigneeId == null ? null : this.employees().find((e) => e.id === assigneeId) ?? null;

    this.savingAssignee.set(true);
    this.assigneeMessage.set(null);
    this.tasksStore.updateTask(
      this.taskId,
      {
        assigneeId,
        assignee: employee?.name ?? '',
      },
      (ok) => {
        this.savingAssignee.set(false);
        this.assigneeMessage.set(ok ? 'saved' : 'failed');
        if (!ok) {
          this.draftAssigneeId.set(task.assigneeId ?? null);
        }
      },
    );
  }

  cancelAssignee(): void {
    const task = this.task();
    this.draftAssigneeId.set(task?.assigneeId ?? null);
    this.assigneeMessage.set(null);
  }

  ngOnInit(): void {
    this.tasksStore.loadFromApi();
    this.employeesStore.loadFromApi();
  }
}
