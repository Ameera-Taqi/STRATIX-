import { Component, computed, HostListener, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LangSwitcherComponent } from '../lang-switcher/lang-switcher.component';
import { ThemeToggleComponent } from '../theme-toggle/theme-toggle.component';
import { NotificationsPanelService } from '../../core/services/notifications-panel.service';
import { NotificationsStore } from '../../core/services/notifications.store';
import { CurrentUserService } from '../../core/services/current-user.service';
import { AuthService } from '../../core/services/auth.service';
import { employeeInitials } from '../../shared/utils/employee.util';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [TranslatePipe, LangSwitcherComponent, ThemeToggleComponent],
  templateUrl: './topbar.component.html',
})
export class TopbarComponent {
  readonly panel = inject(NotificationsPanelService);
  private readonly notificationsStore = inject(NotificationsStore);
  private readonly currentUser = inject(CurrentUserService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly titleKey = input.required<string>();
  readonly unreadCount = this.notificationsStore.unreadCount;
  readonly profile = this.currentUser.profile;
  readonly initials = computed(() => employeeInitials(this.profile().name));
  readonly menuOpen = signal(false);

  onBellClick(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.menuOpen.set(false);
    this.panel.toggle();
  }

  toggleUserMenu(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.panel.close();
    this.menuOpen.update((open) => !open);
  }

  closeUserMenu(): void {
    this.menuOpen.set(false);
  }

  navigateAndClose(path: string | (string | number)[]): void {
    this.closeUserMenu();
    const commands = Array.isArray(path) ? path.map((segment) => String(segment)) : [path];
    void this.router.navigate(commands);
  }

  logout(): void {
    this.closeUserMenu();
    this.auth.logout();
  }

  @HostListener('document:click')
  onDocumentClick(): void {
    this.closeUserMenu();
  }
}
