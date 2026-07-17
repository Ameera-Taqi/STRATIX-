import { Component, computed, inject } from '@angular/core';
import { Router } from '@angular/router';
import { NotificationsPanelService } from '../../core/services/notifications-panel.service';
import { NotificationsStore } from '../../core/services/notifications.store';
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
  readonly recentNotifications = computed(() => this.store.getRecent(5));

  title(n: Parameters<NotificationsStore['displayTitle']>[0]): string {
    return this.store.displayTitle(n);
  }

  body(n: Parameters<NotificationsStore['displayBody']>[0]): string {
    return this.store.displayBody(n);
  }

  close(): void {
    this.panel.close();
  }

  markAllRead(): void {
    this.store.markAllRead();
  }

  openNotification(id: number): void {
    this.store.markAsRead(id);
    this.close();
    void this.router.navigate(['/notifications']);
  }

  viewAll(): void {
    this.close();
    void this.router.navigate(['/notifications']);
  }
}
