import { Component, OnInit, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TasksStore } from '../../core/services/tasks.store';
import { CommentsPanelComponent } from '../../shared/components/comments-panel/comments-panel.component';
import { AttachmentsPanelComponent } from '../../shared/components/attachments-panel/attachments-panel.component';
import { ActivityFeedComponent } from '../../shared/components/activity-feed/activity-feed.component';
import {
  BreadcrumbItem,
  BreadcrumbsComponent,
} from '../../shared/components/breadcrumbs/breadcrumbs.component';
import { priorityClass } from '../../shared/utils/status.util';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [
    TopbarComponent,
    RouterLink,
    TranslatePipe,
    BreadcrumbsComponent,
    CommentsPanelComponent,
    AttachmentsPanelComponent,
    ActivityFeedComponent,
  ],
  templateUrl: './task-detail.component.html',
})
export class TaskDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly tasksStore = inject(TasksStore);

  readonly taskId = Number(this.route.snapshot.paramMap.get('id'));

  readonly task = computed(() => {
    this.tasksStore.tasks();
    return this.tasksStore.getById(this.taskId) ?? null;
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

  readonly priorityClass = priorityClass;
  readonly subtasks: { name: string; progress: number; status: string }[] = [];

  projectLink(): (string | number)[] {
    const t = this.task();
    return t ? ['/projects', t.projectId] : ['/projects'];
  }

  ngOnInit(): void {
    if (!this.tasksStore.loaded()) {
      this.tasksStore.loadFromApi();
    }
  }
}
