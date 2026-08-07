import { Component, HostListener, computed, inject, input, signal } from '@angular/core';
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
import {
  GlobalSearchHit,
  GlobalSearchService,
} from '../../core/services/global-search.service';
import { QuickCreateService } from '../../core/services/quick-create.service';
import { QuickCreateAction } from '../../core/config/quick-create';
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
  readonly search = inject(GlobalSearchService);
  readonly quickCreate = inject(QuickCreateService);

  readonly titleKey = input.required<string>();
  readonly unreadCount = this.notificationsStore.unreadCount;
  readonly profile = this.currentUser.profile;
  readonly initials = computed(() => employeeInitials(this.profile()?.name ?? ''));
  readonly roleKey = roleLabelKey;
  readonly menuOpen = signal(false);
  readonly createOpen = signal(false);

  readonly searchResults = this.search.results;
  readonly searchOpen = this.search.open;
  readonly searchHasResults = this.search.hasResults;
  readonly searchEmptyQuery = this.search.isEmptyQuery;

  toggleNav(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.menuOpen.set(false);
    this.createOpen.set(false);
    this.panel.close();
    this.search.closePanel();
    this.shellNav.toggle();
  }

  onSearchInput(value: string): void {
    this.search.setQuery(value);
  }

  onSearchFocus(): void {
    this.menuOpen.set(false);
    this.createOpen.set(false);
    this.panel.close();
    this.search.openPanel();
  }

  onSearchKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      this.search.closePanel();
      return;
    }
    if (event.key === 'Enter') {
      const r = this.search.results();
      const first = r.projects[0] ?? r.tasks[0] ?? r.people[0];
      if (first) {
        event.preventDefault();
        this.openHit(first);
      }
    }
  }

  openHit(hit: GlobalSearchHit): void {
    this.search.clear();
    void this.router.navigate(hit.route.map(String));
  }

  toggleCreate(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.menuOpen.set(false);
    this.panel.close();
    this.search.closePanel();
    this.createOpen.update((open) => !open);
  }

  async runCreate(action: QuickCreateAction): Promise<void> {
    this.createOpen.set(false);
    await this.quickCreate.run(action);
  }

  onBellClick(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.menuOpen.set(false);
    this.createOpen.set(false);
    this.search.closePanel();
    this.panel.toggle();
  }

  toggleUserMenu(event: MouseEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.panel.close();
    this.search.closePanel();
    this.createOpen.set(false);
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
    this.createOpen.set(false);
    this.search.closePanel();
  }
}
