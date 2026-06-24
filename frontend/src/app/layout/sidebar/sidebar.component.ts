import { Component, computed, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { SIDEBAR_MODULES, SystemModuleCode } from '../../core/config/stratix-modules';
import { RoleAccessService } from '../../core/services/role-access.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, TranslatePipe],
  templateUrl: './sidebar.component.html',
})
export class SidebarComponent {
  private readonly roleAccess = inject(RoleAccessService);

  readonly nav = computed(() =>
    SIDEBAR_MODULES.filter((m) => this.roleAccess.canRead(m.code as SystemModuleCode)).map((m) => ({
      labelKey: m.labelKey,
      path: m.path!,
      icon: iconFor(m.code),
    })),
  );
}

function iconFor(code: string): string {
  const icons: Record<string, string> = {
    DASHBOARD: '◫',
    PROJECTS: '▣',
    STAGES: '▬',
    TASKS: '☑',
    EMPLOYEES: '👥',
    PERFORMANCE: '◔',
    REPORTS: '▤',
    RISKS: '⚠',
    AUDIT: '📜',
    NOTIFICATIONS: '🔔',
    SETTINGS: '⚙',
  };
  return icons[code] ?? '•';
}
