import { Component, inject, OnInit } from '@angular/core';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { NotificationsStore } from '../../core/services/notifications.store';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, UiIconComponent],
  template: `
    <app-topbar titleKey="nav.notifications" />
    <main class="stratix-page p-6">
      <div class="mb-4 flex flex-wrap items-center justify-between gap-3">
        <p class="stratix-muted text-sm">{{ 'module.notificationsHint' | t }}</p>
        @if (store.unreadCount() > 0) {
          <button
            type="button"
            class="rounded-lg border border-primary/30 bg-primary/5 px-3 py-1.5 text-sm font-medium text-primary transition hover:bg-primary/10"
            (click)="markAllRead()"
          >
            {{ 'notifications.markAllRead' | t }}
          </button>
        }
      </div>

      <div class="stratix-card overflow-hidden">
        @if (store.loading() && store.items().length === 0) {
          <div class="px-6 py-14 text-center stratix-muted">{{ 'common.loading' | t }}</div>
        } @else if (store.items().length === 0) {
          <div class="px-6 py-14 text-center">
            <p class="flex justify-center opacity-40"><app-ui-icon name="bell" size="2xl" /></p>
            <p class="mt-2 stratix-muted">{{ 'notifications.empty' | t }}</p>
          </div>
        } @else {
          <ul class="divide-y divide-slate-100 dark:divide-slate-700">
            @for (n of store.items(); track n.id) {
              <li class="flex gap-3 px-5 py-4 transition" [class.stratix-notification-unread]="!n.read">
                <span class="mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full" [class.bg-primary]="!n.read" [class.bg-transparent]="n.read"></span>
                <div class="min-w-0 flex-1">
                  <div class="flex flex-wrap items-start justify-between gap-2">
                    <strong class="text-dark dark:text-slate-100">{{ n.title }}</strong>
                    <span class="text-xs stratix-muted">{{ store.timeAgo(n.createdAt) }}</span>
                  </div>
                  <p class="mt-1 text-sm stratix-muted">{{ n.body }}</p>
                  <div class="mt-2 flex items-center gap-3">
                    @if (!n.read) {
                      <button type="button" class="text-xs font-medium text-primary hover:underline" (click)="markRead(n.id)">
                        {{ 'notifications.markRead' | t }}
                      </button>
                    }
                    <button type="button" class="text-xs font-medium text-danger hover:underline" (click)="remove(n.id)">
                      {{ 'common.delete' | t }}
                    </button>
                  </div>
                </div>
              </li>
            }
          </ul>
        }
      </div>
    </main>
  `,
})
export class NotificationsComponent implements OnInit {
  readonly store = inject(NotificationsStore);

  ngOnInit(): void {
    this.store.loadFromApi();
  }

  markRead(id: number): void {
    this.store.markAsRead(id);
  }

  markAllRead(): void {
    this.store.markAllRead();
  }

  remove(id: number): void {
    this.store.remove(id);
  }
}
