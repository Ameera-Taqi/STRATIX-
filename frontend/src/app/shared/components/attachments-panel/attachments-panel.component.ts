import { Component, computed, inject, input } from '@angular/core';
import { AttachmentsStore } from '../../../core/services/attachments.store';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';

@Component({
  selector: 'app-attachments-panel',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    <div class="space-y-4">
      <label class="flex cursor-pointer items-center justify-center gap-2 rounded-lg border-2 border-dashed border-slate-200 px-4 py-6 transition hover:border-primary/40 hover:bg-primary/5 dark:border-slate-600">
        <span class="text-2xl">📎</span>
        <span class="text-sm font-medium text-primary">{{ 'attachments.upload' | t }}</span>
        <input type="file" class="hidden" (change)="onFile($event)" />
      </label>

      @if (list().length === 0) {
        <p class="text-sm stratix-muted">{{ 'attachments.empty' | t }}</p>
      } @else {
        <ul class="divide-y divide-slate-100 dark:divide-slate-700">
          @for (f of list(); track f.id) {
            <li class="flex items-center justify-between gap-3 py-3">
              <div class="min-w-0">
                <p class="truncate text-sm font-medium text-dark dark:text-slate-100">{{ f.fileName }}</p>
                <p class="text-xs stratix-muted">
                  {{ store.formatSize(f.fileSize) }} · {{ f.uploadedBy }} · {{ formatAt(f.uploadedAt) }}
                </p>
              </div>
              <button type="button" class="shrink-0 text-xs text-danger hover:underline" (click)="remove(f.id)">
                {{ 'common.delete' | t }}
              </button>
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class AttachmentsPanelComponent {
  readonly store = inject(AttachmentsStore);

  readonly projectId = input.required<number>();
  readonly projectName = input.required<string>();
  readonly taskId = input<number>();

  readonly list = computed(() => {
    this.store.files();
    const tid = this.taskId();
    if (tid != null) return this.store.forTask(tid);
    return this.store.forProject(this.projectId());
  });

  onFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    this.store.upload(this.projectId(), this.projectName(), file, this.taskId());
    input.value = '';
  }

  remove(id: number): void {
    this.store.remove(id);
  }

  formatAt(iso: string): string {
    return new Date(iso).toLocaleString();
  }
}
