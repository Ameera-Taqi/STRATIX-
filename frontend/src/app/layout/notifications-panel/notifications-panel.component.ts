import { Component, computed, effect, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationsPanelService } from '../../core/services/notifications-panel.service';
import { NotificationsStore, NotificationItem } from '../../core/services/notifications.store';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

@Component({
  selector: 'app-notifications-panel',
  standalone: true,
  imports: [TranslatePipe, UiIconComponent],
  templateUrl: './notifications-panel.component.html',
})
export class NotificationsPanelComponent {
  private readonly panel = inject(NotificationsPanelService);
  readonly store = inject(NotificationsStore);
  private readonly router = inject(Router);

  readonly isOpen = this.panel.open;
  readonly unreadCount = this.store.unreadCount;
  readonly recentNotifications = computed(() => this.store.getRecent(8));

  constructor() {
    effect(() => {
      if (this.isOpen()) this.store.refresh();
    });
  }

  title(n: NotificationItem): string {
    return this.store.displayTitle(n);
  }

  entityLabel(n: NotificationItem): string | null {
    return this.store.displayEntityLabel(n);
  }

  project(n: NotificationItem): string | null {
    return this.store.displayProject(n);
  }

  body(n: NotificationItem): string {
    return this.store.displayBody(n);
  }

  ctaKey(n: NotificationItem): string | null {
    return this.store.ctaKey(n);
  }

  close(): void {
    this.panel.close();
  }

  markAllRead(): void {
    this.store.markAllRead();
  }

  openNotification(n: NotificationItem, event?: Event): void {
    event?.stopPropagation();
    this.close();
    this.store.openNotification(n);
  }

  openCta(n: NotificationItem, event: Event): void {
    event.stopPropagation();
    this.openNotification(n);
  }

  viewAll(): void {
    this.close();
    void this.router.navigate(['/notifications']);
  }
}
