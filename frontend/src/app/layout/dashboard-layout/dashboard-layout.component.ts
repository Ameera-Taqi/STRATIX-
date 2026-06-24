import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from '../sidebar/sidebar.component';

@Component({
  selector: 'app-dashboard-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent],
  templateUrl: './dashboard-layout.component.html',
})
export class DashboardLayoutComponent {}
