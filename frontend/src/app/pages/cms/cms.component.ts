import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RoleAccessService } from '../../core/services/role-access.service';
import { CmsPermissionsStore } from '../../core/services/cms-permissions.store';
import { UpdateCompanyAdminModulePermissionItem } from '../../core/models/cms.model';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

@Component({
  selector: 'app-cms',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, UiIconComponent, RouterLink],
  templateUrl: './cms.component.html',
})
export class CmsComponent implements OnInit {
  private readonly store = inject(CmsPermissionsStore);
  private readonly roleAccess = inject(RoleAccessService);

  readonly isSuperAdmin = computed(() => this.roleAccess.role() === 'SUPER_ADMIN');
  readonly loading = this.store.loading;
  readonly saving = this.store.saving;
  readonly error = this.store.error;
  readonly visibleCount = this.store.visibleCount;

  readonly draft = signal<UpdateCompanyAdminModulePermissionItem[]>([]);
  readonly savedFlash = signal(false);

  readonly totalCount = computed(() => this.draft().length);
  readonly draftVisibleCount = computed(() => this.draft().filter((d) => d.visibleToCompanyAdmin).length);

  ngOnInit(): void {
    if (!this.isSuperAdmin()) return;
    void this.reload();
  }

  async reload(): Promise<void> {
    await this.store.load();
    this.syncDraftFromStore();
  }

  toggleVisible(code: string, checked: boolean): void {
    this.draft.update((rows) =>
      rows.map((r) =>
        r.moduleCode === code
          ? {
              ...r,
              visibleToCompanyAdmin: checked,
              writableByCompanyAdmin: checked ? r.writableByCompanyAdmin : false,
            }
          : r,
      ),
    );
  }

  toggleWritable(code: string, checked: boolean): void {
    this.draft.update((rows) =>
      rows.map((r) => {
        if (r.moduleCode !== code) return r;
        if (!r.visibleToCompanyAdmin) return { ...r, writableByCompanyAdmin: false };
        return { ...r, writableByCompanyAdmin: checked };
      }),
    );
  }

  async save(): Promise<void> {
    const ok = await this.store.save(this.draft());
    if (ok) {
      this.syncDraftFromStore();
      this.savedFlash.set(true);
      setTimeout(() => this.savedFlash.set(false), 2500);
    }
  }

  resetDraft(): void {
    this.syncDraftFromStore();
  }

  moduleLabelKey(code: string): string {
    const map: Record<string, string> = {
      DASHBOARD: 'nav.dashboard',
      PROJECTS: 'nav.projects',
      STAGES: 'module.stages',
      TASKS: 'nav.tasks',
      EMPLOYEES: 'nav.team',
      PERFORMANCE: 'nav.performance',
      REPORTS: 'nav.reports',
      NOTIFICATIONS: 'nav.notifications',
      RISKS: 'nav.risks',
      AUDIT: 'nav.audit',
      SETTINGS: 'nav.settings',
      ROLES: 'nav.roles',
      DEPARTMENTS: 'nav.departments',
      BRANDING: 'nav.branding',
    };
    return map[code] ?? code;
  }

  private syncDraftFromStore(): void {
    this.draft.set(
      this.store.permissions().map((p) => ({
        moduleCode: p.moduleCode,
        visibleToCompanyAdmin: p.visibleToCompanyAdmin,
        writableByCompanyAdmin: p.writableByCompanyAdmin,
      })),
    );
  }
}
