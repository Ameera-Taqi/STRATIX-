import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationsPanelComponent } from './layout/notifications-panel/notifications-panel.component';
import { ApiErrorToastComponent } from './shared/components/api-error-toast.component';
import { ThemeService } from './core/theme/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NotificationsPanelComponent, ApiErrorToastComponent],
  template: `
    <a href="#main-content" class="sr-only focus:not-sr-only focus:absolute focus:start-4 focus:top-4 focus:z-[200] focus:rounded-lg focus:bg-primary focus:px-3 focus:py-2 focus:text-white">
      Skip to content
    </a>
    <router-outlet />
    <app-notifications-panel />
    <app-api-error-toast />
  `,
})
export class AppComponent {
  /** Eagerly apply saved/default theme on boot (also via APP_INITIALIZER). */
  private readonly theme = inject(ThemeService);
}
