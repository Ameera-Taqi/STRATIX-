import { Component, inject } from '@angular/core';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { NotificationsStore } from '../../core/services/notifications.store';
import { LanguageService } from '../../core/i18n/language.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-notifications',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe],
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
        @if (store.items().length === 0) {
          <div class="px-6 py-14 text-center">
            <p class="text-3xl opacity-40">🔔</p>
            <p class="mt-2 stratix-muted">{{ 'notifications.empty' | t }}</p>
          </div>
        } @else {
          <ul class="divide-y divide-slate-100 dark:divide-slate-700">
            @for (n of store.items(); track n.id) {
              <li class="flex gap-3 px-5 py-4 transition" [class.stratix-notification-unread]="!n.read">
                <span class="mt-1.5 h-2.5 w-2.5 shrink-0 rounded-full" [class.bg-primary]="!n.read" [class.bg-transparent]="n.read"></span>
                <div class="min-w-0 flex-1">
                  <div class="flex flex-wrap items-start justify-between gap-2">
                    <strong class="text-dark dark:text-slate-100">{{ title(n) }}</strong>
                    <span class="text-xs stratix-muted">{{ store.timeAgo(n.createdAt) }}</span>
                  </div>
                  <p class="mt-1 text-sm stratix-muted">{{ body(n) }}</p>
                  @if (!n.read) {
                    <button type="button" class="mt-2 text-xs font-medium text-primary hover:underline" (click)="markRead(n.id)">
                      {{ 'notifications.markRead' | t }}
                    </button>
                  }
                </div>
              </li>
            }
          </ul>
        }
      </div>
    </main>
  `,
})
export class NotificationsComponent {
  readonly store = inject(NotificationsStore);
  private readonly lang = inject(LanguageService);

  title(n: Parameters<NotificationsStore['displayTitle']>[0]): string {
    return this.store.displayTitle(n, (k) => this.lang.t(k));
  }

  body(n: Parameters<NotificationsStore['displayBody']>[0]): string {
    return this.store.displayBody(n, (k) => this.lang.t(k));
  }

  markRead(id: number): void {
    this.store.markAsRead(id);
  }

  markAllRead(): void {
    this.store.markAllRead();
  }
}
