import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommentsStore } from '../../../core/services/comments.store';
import { TasksStore } from '../../../core/services/tasks.store';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-comments-panel',
  standalone: true,
  imports: [FormsModule, TranslatePipe],
  template: `
    <div class="space-y-4">
      <form class="flex gap-2" (ngSubmit)="submit()">
        <input
          type="text"
          class="stratix-input min-w-0 flex-1 px-3 py-2 text-sm"
          [(ngModel)]="text"
          name="comment"
          [placeholder]="'comments.placeholder' | t"
          required
        />
        <button type="submit" class="shrink-0 rounded-lg bg-primary px-4 py-2 text-sm text-white hover:bg-primary/90">
          {{ 'comments.add' | t }}
        </button>
      </form>
      @if (error()) {
        <p class="text-xs text-danger">{{ error()! | t }}</p>
      }
      @if (store.loading() && list().length === 0) {
        <p class="text-sm stratix-muted">{{ 'common.loading' | t }}</p>
      } @else if (list().length === 0) {
        <p class="text-sm stratix-muted">{{ 'comments.empty' | t }}</p>
      } @else {
        <ul class="space-y-3">
          @for (c of list(); track c.id) {
            <li class="rounded-lg bg-slate-50 px-4 py-3 dark:bg-slate-800/60">
              <div class="flex items-center justify-between gap-2">
                <span class="text-sm font-medium text-dark dark:text-slate-100">{{ c.author }}</span>
                <div class="flex items-center gap-2">
                  <span class="text-[10px] stratix-muted">{{ formatAt(c.createdAt) }}</span>
                  <button type="button" class="text-[10px] text-danger hover:underline" (click)="remove(c.id)">
                    {{ 'common.delete' | t }}
                  </button>
                </div>
              </div>
              <p class="mt-1 text-sm text-slate-600 dark:text-slate-300">{{ c.text }}</p>
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class CommentsPanelComponent {
  readonly store = inject(CommentsStore);
  private readonly tasks = inject(TasksStore);

  readonly taskId = input.required<number>();
  readonly taskTitle = input.required<string>();

  text = '';
  readonly error = signal<string | null>(null);

  readonly list = computed(() => {
    this.store.comments();
    return this.store.forTask(this.taskId());
  });

  constructor() {
    effect(() => this.store.loadForTask(this.taskId()));
  }

  submit(): void {
    if (!this.text.trim()) {
      this.error.set('comments.errorEmpty');
      return;
    }
    const task = this.tasks.getById(this.taskId());
    this.store.add(this.taskId(), this.taskTitle(), this.text, task?.projectId, task?.projectName);
    this.text = '';
    this.error.set(null);
  }

  remove(id: number): void {
    this.store.remove(id);
  }

  formatAt(iso: string): string {
    return new Date(iso).toLocaleString();
  }
}
