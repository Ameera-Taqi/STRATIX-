import { Component, computed, inject, input, OnInit } from '@angular/core';
import { AuditLogStore } from '../../../core/services/audit-log.store';
import { TranslatePipe } from '../../../core/i18n/translate.pipe';
import { AuditEntry } from '../../../core/models/audit.model';
import { activityEventMeta } from '../../utils/activity-event.util';

@Component({
  selector: 'app-activity-timeline',
  standalone: true,
  imports: [TranslatePipe],
  template: `
    @if (loading()) {
      <p class="text-sm stratix-muted">{{ 'audit.loading' | t }}</p>
    } @else if (error()) {
      <p class="text-sm text-danger">{{ error()! | t }}</p>
    } @else if (entries().length === 0) {
      <p class="text-sm stratix-muted">{{ 'activity.empty' | t }}</p>
    } @else {
      <ol class="relative space-y-0">
        @for (e of entries(); track e.id; let last = $last) {
          <li class="relative flex gap-4 pb-8" [class.pb-0]="last">
            @if (!last) {
              <span
                class="absolute start-[15px] top-8 h-[calc(100%-8px)] w-0.5 bg-slate-200 dark:bg-slate-700"
                aria-hidden="true"
              ></span>
            }
            <span
              class="relative z-10 flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-sm ring-4"
              [class]="meta(e).iconClass"
            >
              {{ meta(e).icon }}
            </span>
            <div class="min-w-0 flex-1 pt-0.5">
              <p class="text-sm font-semibold text-dark dark:text-slate-100">
                {{ eventLabel(e) | t }}
              </p>
              <p class="mt-0.5 text-sm text-primary">{{ e.entityLabel }}</p>
              @if (subtitle(e); as sub) {
                <p class="mt-1 text-xs text-slate-600 dark:text-slate-300">{{ sub }}</p>
              }
              <p class="mt-2 text-xs stratix-muted">
                {{ e.actor }}
                @if (e.actorRole) {
                  <span> · {{ e.actorRole }}</span>
                }
                <span> · {{ formatAt(e.at) }}</span>
              </p>
            </div>
          </li>
        }
      </ol>
    }
  `,
})
export class ActivityTimelineComponent implements OnInit {
  private readonly audit = inject(AuditLogStore);

  readonly projectId = input.required<number>();
  readonly limit = input(50);

  readonly loading = this.audit.loading;
  readonly error = this.audit.error;

  readonly entries = computed(() => {
    this.audit.entries();
    return this.audit.forProject(this.projectId()).slice(0, this.limit());
  });

  ngOnInit(): void {
    this.audit.loadFromApi({
      projectId: this.projectId(),
      page: 0,
      size: this.limit(),
    });
  }

  meta(entry: AuditEntry) {
    return activityEventMeta(entry);
  }

  eventLabel(entry: AuditEntry): string {
    const key = this.meta(entry).key;
    return key === 'OTHER' ? 'activity.event.other' : `activity.event.${key}`;
  }

  subtitle(entry: AuditEntry): string | null {
    if (entry.field === 'assignee' && entry.newValue) {
      return `→ ${entry.newValue}`;
    }
    if (entry.field === 'status' && entry.oldValue && entry.newValue) {
      return `${this.formatValue(entry.oldValue)} → ${this.formatValue(entry.newValue)}`;
    }
    if (entry.details && !entry.field) {
      return entry.details;
    }
    return null;
  }

  formatValue(value: string): string {
    return value.replace(/_/g, ' ');
  }

  formatAt(iso: string): string {
    const date = new Date(iso);
    const now = Date.now();
    const diff = now - date.getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'just now';
    if (mins < 60) return `${mins}m ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    if (days < 7) return `${days}d ago`;
    return date.toLocaleString();
  }
}
