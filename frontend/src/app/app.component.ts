import { Component, inject, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { NotificationsPanelComponent } from './layout/notifications-panel/notifications-panel.component';
import { AuthService } from './core/services/auth.service';
import { DataBootstrapService } from './core/services/data-bootstrap.service';
import { ThemeService } from './core/theme/theme.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NotificationsPanelComponent],
  template: `
    <router-outlet />
    <app-notifications-panel />
  `,
})
export class AppComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly bootstrap = inject(DataBootstrapService);
  /** Eagerly apply saved/default theme on boot. */
  private readonly theme = inject(ThemeService);

  ngOnInit(): void {
    this.auth.restoreSession().subscribe(() => {
      if (this.auth.isAuthenticated()) {
        this.bootstrap.bootstrap();
      }
    });
  }
}
