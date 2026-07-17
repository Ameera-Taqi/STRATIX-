import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RoleAccessService } from '../../core/services/role-access.service';
import { DepartmentsStore } from '../../core/services/departments.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { CmsPermissionsStore } from '../../core/services/cms-permissions.store';
import {
  STRATIX_MODULES,
  SystemModuleCode,
  canAccessModule,
} from '../../core/config/stratix-modules';
import {
  roleLabelKey,
  visibleRolesFor,
} from '../../core/config/stratix-roles';
import { UserRole } from '../../core/models/user.model';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

type RolesTab = 'roles' | 'access' | 'departments';

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, UiIconComponent],
  templateUrl: './roles.component.html',
})
export class RolesComponent implements OnInit {
  private readonly roleAccess = inject(RoleAccessService);
  private readonly departmentsStore = inject(DepartmentsStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly cms = inject(CmsPermissionsStore);

  readonly tab = signal<RolesTab>('roles');
  readonly canWrite = computed(() => this.roleAccess.canWrite('ROLES'));
  readonly callerRole = computed(() => this.roleAccess.role());

  readonly roles = computed(() => visibleRolesFor(this.callerRole()));
  readonly roleKey = roleLabelKey;

  readonly matrixModules = computed(() =>
    STRATIX_MODULES.filter(
      (m) =>
        m.code !== 'AUTH' &&
        (this.callerRole() === 'SUPER_ADMIN' ||
          (m.code !== 'ORGANIZATIONS' && m.code !== 'PLATFORM_CMS')),
    ),
  );

  readonly matrixRoles = computed(() => this.roles().map((r) => r.code));

  readonly departments = this.departmentsStore.departments;
  readonly deptLoading = this.departmentsStore.loading;
  readonly deptError = this.departmentsStore.error;
  readonly deptSaving = this.departmentsStore.saving;

  readonly showDeptModal = signal(false);
  readonly editingDeptId = signal<number | null>(null);
  readonly deptFormError = signal<string | null>(null);
  deptForm = { name: '', description: '' };

  ngOnInit(): void {
    void this.cms.load();
    void this.departmentsStore.load();
  }

  setTab(tab: RolesTab): void {
    this.tab.set(tab);
  }

  cellAccess(moduleCode: SystemModuleCode, role: UserRole): 'RW' | 'R' | '—' {
    const module = STRATIX_MODULES.find((m) => m.code === moduleCode);
    if (!module) return '—';
    const cmsMap =
      role === 'ORG_ADMIN' || role === 'ADMIN' ? this.cms.map() : null;
    const canRead = canAccessModule(module, role, 'read', cmsMap);
    const canWrite = canAccessModule(module, role, 'write', cmsMap);
    if (canWrite) return 'RW';
    if (canRead) return 'R';
    return '—';
  }

  openAddDepartment(): void {
    this.editingDeptId.set(null);
    this.deptForm = { name: '', description: '' };
    this.deptFormError.set(null);
    this.showDeptModal.set(true);
  }

  openEditDepartment(id: number, name: string, description: string | null): void {
    this.editingDeptId.set(id);
    this.deptForm = { name, description: description ?? '' };
    this.deptFormError.set(null);
    this.showDeptModal.set(true);
  }

  closeDeptModal(): void {
    this.showDeptModal.set(false);
    this.deptFormError.set(null);
  }

  async saveDepartment(): Promise<void> {
    if (!this.deptForm.name.trim()) {
      this.deptFormError.set('roles.deptNameRequired');
      return;
    }
    const id = this.editingDeptId();
    const ok =
      id == null
        ? await this.departmentsStore.create(this.deptForm.name, this.deptForm.description)
        : await this.departmentsStore.update(id, this.deptForm.name, this.deptForm.description);
    if (ok) {
      this.closeDeptModal();
      this.employeesStore.reloadDepartments();
    }
  }

  async deleteDepartment(id: number): Promise<void> {
    const ok = await this.departmentsStore.remove(id);
    if (ok) this.employeesStore.reloadDepartments();
  }
}
