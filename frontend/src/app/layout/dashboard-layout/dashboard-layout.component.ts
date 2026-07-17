import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { ShellNavService } from '../../core/services/shell-nav.service';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TranslatePipe],
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent {
  readonly shellNav = inject(ShellNavService);
}
