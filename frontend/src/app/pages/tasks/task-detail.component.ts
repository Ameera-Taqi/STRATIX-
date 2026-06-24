import { Component, computed, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TasksStore } from '../../core/services/tasks.store';
import { CommentsPanelComponent } from '../../shared/components/comments-panel/comments-panel.component';
import { AttachmentsPanelComponent } from '../../shared/components/attachments-panel/attachments-panel.component';
import { ActivityFeedComponent } from '../../shared/components/activity-feed/activity-feed.component';
import { priorityClass } from '../../shared/utils/status.util';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-task-detail',
  standalone: true,
  imports: [
    TopbarComponent,
    RouterLink,
    TranslatePipe,
    CommentsPanelComponent,
    AttachmentsPanelComponent,
    ActivityFeedComponent,
  ],
  templateUrl: './task-detail.component.html',
})
export class TaskDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly tasksStore = inject(TasksStore);

  readonly taskId = Number(this.route.snapshot.paramMap.get('id'));

  readonly task = computed(() => {
    this.tasksStore.tasks();
    return this.tasksStore.getById(this.taskId) ?? this.tasksStore.getAll()[0];
  });

  readonly priorityClass = priorityClass;

  readonly subtasks: { name: string; progress: number; status: string }[] = [];

  projectLink(): (string | number)[] {
    return ['/projects', this.task().projectId];
  }
}
