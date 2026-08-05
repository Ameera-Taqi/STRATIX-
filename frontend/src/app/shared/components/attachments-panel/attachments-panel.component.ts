import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AttachmentsStore } from '../../../core/services/attachments.store';
import { detectFileType, FILE_TYPES } from '../../../core/models/project-file.model';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { UiIconComponent } from '../ui-icon/ui-icon.component';

@Component({
  selector: 'app-attachments-panel',
  standalone: true,
  imports: [TranslatePipe, UiIconComponent, FormsModule],
  template: `
    <div class="space-y-4">
      @if (!pendingFile()) {
        <label
          class="flex cursor-pointer items-center justify-center gap-2 rounded-lg border-2 border-dashed border-slate-200 px-4 py-6 transition hover:border-primary/40 hover:bg-primary/5 dark:border-slate-600"
        >
          <app-ui-icon name="paperclip" size="lg" className="text-primary" />
          <span class="text-sm font-medium text-primary">{{ 'attachments.upload' | t }}</span>
          <input type="file" class="hidden" (change)="onFileSelected($event)" />
        </label>
      } @else {
        <div class="stratix-card border border-slate-200 p-5 dark:border-slate-700">
          <h4 class="stratix-heading text-sm">{{ 'attachments.detailsTitle' | t }}</h4>
          <p class="mt-1 text-xs stratix-muted">{{ 'attachments.detailsHint' | t }}</p>

          <div class="mt-3 rounded-lg bg-slate-50 px-3 py-2 text-sm dark:bg-slate-800/60">
            <p class="font-medium text-dark dark:text-slate-100">{{ pendingFile()!.name }}</p>
            <p class="text-xs stratix-muted">{{ store.formatSize(pendingFile()!.size) }}</p>
          </div>

          @if (formError()) {
            <p class="mt-3 rounded-lg bg-red-50 px-3 py-2 text-sm text-danger dark:bg-red-900/30">{{ formError()! | t }}</p>
          }

          <form class="mt-4 grid gap-4 sm:grid-cols-2" (ngSubmit)="confirmUpload()">
            <div class="sm:col-span-2">
              <label class="stratix-muted mb-1 block text-sm">{{ 'attachments.displayName' | t }} *</label>
              <input
                type="text"
                class="stratix-input w-full px-3 py-2 text-sm"
                [(ngModel)]="form.displayName"
                name="displayName"
                required
              />
            </div>
            <div class="sm:col-span-2">
              <label class="stratix-muted mb-1 block text-sm">{{ 'attachments.fileType' | t }}</label>
              <select class="stratix-input w-full px-3 py-2 text-sm" [(ngModel)]="form.fileType" name="fileType">
                @for (t of fileTypes; track t) {
                  <option [ngValue]="t">{{ ('attachments.fileType.' + t) | t }}</option>
                }
              </select>
            </div>
            <div class="sm:col-span-2">
              <label class="stratix-muted mb-1 block text-sm">{{ 'attachments.description' | t }}</label>
              <textarea
                class="stratix-input min-h-[88px] w-full px-3 py-2 text-sm"
                [(ngModel)]="form.description"
                name="description"
                [placeholder]="'attachments.descriptionPlaceholder' | t"
                rows="3"
              ></textarea>
            </div>
            <div class="flex flex-wrap justify-end gap-2 sm:col-span-2">
              <button type="button" class="stratix-input px-4 py-2 text-sm" [disabled]="store.uploading()" (click)="cancelPending()">
                {{ 'projects.cancel' | t }}
              </button>
              <button
                type="submit"
                class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-white disabled:opacity-60"
                [disabled]="store.uploading()"
              >
                {{ store.uploading() ? ('common.loading' | t) : ('attachments.confirmUpload' | t) }}
              </button>
            </div>
          </form>
        </div>
      }

      @if (store.loading() && list().length === 0) {
        <p class="text-sm stratix-muted">{{ 'common.loading' | t }}</p>
      } @else if (list().length === 0) {
        <p class="text-sm stratix-muted">{{ 'attachments.empty' | t }}</p>
      } @else {
        <ul class="divide-y divide-slate-100 dark:divide-slate-700">
          @for (f of list(); track f.id) {
            <li class="flex items-start justify-between gap-3 py-3">
              <div class="min-w-0">
                <div class="flex flex-wrap items-center gap-2">
                  <p class="truncate text-sm font-medium text-dark dark:text-slate-100">{{ f.fileName }}</p>
                  @if (f.category) {
                    <span class="rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-medium text-slate-600 dark:bg-slate-700 dark:text-slate-300">
                      {{ fileTypeLabel(f.category) | t }}
                    </span>
                  }
                </div>
                @if (f.description) {
                  <p class="mt-1 text-xs text-slate-600 dark:text-slate-300">{{ f.description }}</p>
                }
                <p class="mt-1 text-xs stratix-muted">
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

  readonly fileTypes = FILE_TYPES;
  readonly pendingFile = signal<File | null>(null);
  readonly formError = signal<string | null>(null);

  form = {
    displayName: '',
    description: '',
    fileType: 'Other' as string,
  };

  readonly list = computed(() => {
    this.store.files();
    return this.store.forProject(this.projectId());
  });

  constructor() {
    effect(() => this.store.loadForProject(this.projectId()));
  }

  fileTypeLabel(value: string): string {
    const known = FILE_TYPES.find((t) => t === value);
    return known ? `attachments.fileType.${known}` : value;
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    input.value = '';
    if (!file) return;
    this.formError.set(null);
    this.form = {
      displayName: file.name,
      description: '',
      fileType: detectFileType(file),
    };
    this.pendingFile.set(file);
  }

  cancelPending(): void {
    this.pendingFile.set(null);
    this.formError.set(null);
  }

  confirmUpload(): void {
    const file = this.pendingFile();
    if (!file) return;
    if (!this.form.displayName.trim()) {
      this.formError.set('attachments.errorName');
      return;
    }
    this.formError.set(null);
    this.store.upload(
      this.projectId(),
      this.projectName(),
      file,
      {
        displayName: this.form.displayName,
        description: this.form.description,
        fileType: this.form.fileType,
      },
      (ok) => {
        if (ok) {
          this.pendingFile.set(null);
        } else {
          this.formError.set('attachments.errorUpload');
        }
      },
    );
  }

  remove(id: number): void {
    this.store.remove(id);
  }

  formatAt(iso: string): string {
    return new Date(iso).toLocaleString();
  }
}
