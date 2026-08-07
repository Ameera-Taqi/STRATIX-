import { Component, computed, HostListener, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LangSwitcherComponent } from '../lang-switcher/lang-switcher.component';
import { ThemeToggleComponent } from '../theme-toggle/theme-toggle.component';
import { NotificationsPanelService } from '../../core/services/notifications-panel.service';
import { NotificationsStore } from '../../core/services/notifications.store';
import { CurrentUserService } from '../../core/services/current-user.service';
import { AuthService } from '../../core/services/auth.service';
import { ShellNavService } from '../../core/services/shell-nav.service';
import { employeeInitials } from '../../shared/utils/employee.util';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';
import { roleLabelKey } from '../../core/config/stratix-roles';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [TranslatePipe, LangSwitcherComponent, ThemeToggleComponent, UiIconComponent, FormsModule],
  templateUrl: './topbar.component.html',
})
export class TopbarComponent {
  readonly panel = inject(NotificationsPanelService);
  private readonly notificationsStore = inject(NotificationsStore);
  private readonly currentUser = inject(CurrentUserService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  readonly shellNav = inject(ShellNavService);

  readonly titleKey = input.required<string>();
  readonly unreadCount = this.notificationsStore.unreadCount;
  readonly profile = this.currentUser.profile;
  readonly initials = computed(() => employeeInitials(this.profile()?.name ?? ''));
  readonly roleKey = roleLabelKey;
  readonly menuOpen = signal(false);
  searchTerm = '';

  toggleNav(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.menuOpen.set(false);
    this.panel.close();
    this.shellNav.toggle();
  }
  onSearch(): void {
    const term = this.searchTerm.trim();
    if (!term) return;
    void this.router.navigate(['/projects'], { queryParams: { q: term } });
  }

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
