import { Component, computed, inject, input, OnInit } from '@angular/core';
import { AuditLogStore } from '../../../core/services/audit-log.store';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AuditEntity } from '../../../core/models/audit.model';
import { UiIconComponent } from '../ui-icon/ui-icon.component';

@Component({
  selector: 'app-activity-feed',
  standalone: true,
  imports: [TranslatePipe, UiIconComponent],
  template: `
    @if (loading()) {
      <p class="text-sm stratix-muted">{{ 'audit.loading' | t }}</p>
    } @else if (error()) {
      <p class="text-sm text-danger">{{ error()! | t }}</p>
    } @else if (entries().length === 0) {
      <p class="text-sm stratix-muted">{{ 'audit.empty' | t }}</p>
    } @else {
      <ul class="space-y-3">
        @for (e of entries(); track e.id) {
          <li class="flex gap-3 rounded-lg border border-slate-100 px-3 py-3 dark:border-slate-700">
            <span class="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-primary/10 text-primary">
              <app-ui-icon [name]="actionIcon(e.action)" size="xs" />
            </span>
            <div class="min-w-0 flex-1">
              <p class="text-sm text-dark dark:text-slate-100">
                <strong>{{ e.actor }}</strong>
              </p>
              <p class="mt-0.5 text-sm">
                {{ 'audit.action.' + e.action | t }}
                <span class="font-medium text-primary">{{ e.entityLabel }}</span>
                @if (e.projectName) {
                  <span class="text-xs stratix-muted"> ({{ e.projectName }})</span>
                }
              </p>
              @if (e.field && e.oldValue && e.newValue) {
                <p class="mt-1 rounded bg-slate-50 px-2 py-1 font-mono text-xs text-slate-600 dark:bg-slate-800/60 dark:text-slate-300">
                  {{ 'audit.field.' + e.field | t }}: {{ formatValue(e.oldValue) }} → {{ formatValue(e.newValue) }}
                </p>
              } @else {
                <p class="mt-0.5 text-xs stratix-muted">{{ e.details }}</p>
              }
              <p class="mt-1 text-[10px] text-slate-400">{{ formatAt(e.at) }}</p>
            </div>
          </li>
        }
      </ul>
    }
  `,
})
export class ActivityFeedComponent implements OnInit {
  private readonly audit = inject(AuditLogStore);

  readonly entityType = input<AuditEntity>();
  readonly entityId = input<number>();
  readonly projectId = input<number>();
  readonly limit = input(20);

  readonly loading = this.audit.loading;
  readonly error = this.audit.error;

  readonly entries = computed(() => {
    this.audit.entries();
    const pid = this.projectId();
    if (pid != null) return this.audit.forProject(pid).slice(0, this.limit());
    const type = this.entityType();
    const id = this.entityId();
    if (type && id != null) return this.audit.forEntity(type, id).slice(0, this.limit());
    return this.audit.entries().slice(0, this.limit());
  });

  ngOnInit(): void {
    const type = this.entityType();
    const id = this.entityId();
    const pid = this.projectId();
    this.audit.loadFromApi({
      entityType: type,
      entityId: id ?? undefined,
      projectId: pid ?? undefined,
      page: 1,
      size: this.limit(),
    });
  }

  actionIcon(action: string): string {
    const icons: Record<string, string> = {
      CREATE: 'plus',
      UPDATE: 'pencil',
      DELETE: 'close',
      STATUS_CHANGE: 'swap',
      ASSIGNMENT_CHANGE: 'user',
      PRIORITY_CHANGE: 'alert-circle',
      MOVE: 'swap',
      COMMENT: 'message',
      UPLOAD: 'paperclip',
      EXPORT: 'file',
    };
    return icons[action] ?? 'dot';
  }

  formatValue(value: string): string {
    return value.replace(/_/g, ' ');
  }

  formatAt(iso: string): string {
    return new Date(iso).toLocaleString();
  }
}
