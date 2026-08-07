import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RoleAccessService } from '../../core/services/role-access.service';
import { DepartmentsStore } from '../../core/services/departments.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { CmsPermissionsStore } from '../../core/services/cms-permissions.store';
import { OrganizationRolesStore } from '../../core/services/organization-roles.store';
import {
  STRATIX_MODULES,
  SystemModuleCode,
  canAccessModule,
} from '../../core/config/stratix-modules';
import {
  assignableRolesFor,
  roleLabelKey,
  visibleRolesFor,
} from '../../core/config/stratix-roles';
import { UserRole } from '../../core/models/user.model';
import { OrganizationRoleRow } from '../../core/models/organization-role.model';
import { UiIconComponent } from '../../shared/components/ui-icon/ui-icon.component';

type RolesTab = 'roles' | 'access' | 'departments';

type MatrixColumn =
  | { kind: 'system'; code: UserRole; labelKey: string }
  | { kind: 'custom'; id: number; code: string; name: string; baseRole: UserRole };

@Component({
  selector: 'app-roles',
  standalone: true,
  imports: [TopbarComponent, TranslatePipe, FormsModule, UiIconComponent],
  templateUrl: './roles.component.html',
})
export class RolesComponent implements OnInit {
  private readonly roleAccess = inject(RoleAccessService);
  private readonly departmentsStore = inject(DepartmentsStore);
  private readonly orgRolesStore = inject(OrganizationRolesStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly cms = inject(CmsPermissionsStore);

  readonly tab = signal<RolesTab>('roles');
  readonly canWrite = computed(() => this.roleAccess.canWrite('ROLES'));
  readonly callerRole = computed(() => this.roleAccess.role());

  readonly systemRoles = computed(() => {
    const role = this.callerRole();
    return role ? visibleRolesFor(role) : [];
  });
  readonly customRoles = this.orgRolesStore.roles;
  readonly customLoading = this.orgRolesStore.loading;
  readonly customError = this.orgRolesStore.error;
  readonly customSaving = this.orgRolesStore.saving;

  readonly roleKey = roleLabelKey;
  readonly baseRoleOptions = computed(() => {
    const role = this.callerRole();
    return role ? assignableRolesFor(role).filter((r) => r !== 'SUPER_ADMIN') : [];
  });

  readonly matrixModules = computed(() =>
    STRATIX_MODULES.filter(
      (m) =>
        m.code !== 'AUTH' &&
        (this.callerRole() === 'SUPER_ADMIN' ||
          (m.code !== 'ORGANIZATIONS' && m.code !== 'PLATFORM_CMS')),
    ),
  );

  readonly matrixColumns = computed((): MatrixColumn[] => {
    const system = this.systemRoles().map(
      (r): MatrixColumn => ({ kind: 'system', code: r.code, labelKey: r.labelKey }),
    );
    const custom = this.customRoles().map(
      (r): MatrixColumn => ({
        kind: 'custom',
        id: r.id,
        code: r.code,
        name: r.name,
        baseRole: r.baseRole as UserRole,
      }),
    );
    return [...system, ...custom];
  });

  readonly departments = this.departmentsStore.departments;
  readonly deptLoading = this.departmentsStore.loading;
  readonly deptError = this.departmentsStore.error;
  readonly deptSaving = this.departmentsStore.saving;

  readonly showDeptModal = signal(false);
  readonly editingDeptId = signal<number | null>(null);
  readonly deptFormError = signal<string | null>(null);
  deptForm = { name: '', description: '' };

  readonly showRoleModal = signal(false);
  readonly editingRoleId = signal<number | null>(null);
  readonly roleFormError = signal<string | null>(null);
  roleForm = { name: '', code: '', description: '', baseRole: 'EMPLOYEE' as string };

  ngOnInit(): void {
    void this.cms.load();
    void this.departmentsStore.load();
    void this.orgRolesStore.load();
  }

  setTab(tab: RolesTab): void {
    this.tab.set(tab);
  }

  cellAccessForColumn(moduleCode: SystemModuleCode, col: MatrixColumn): 'RW' | 'R' | '—' {
    const role = col.kind === 'system' ? col.code : col.baseRole;
    return this.cellAccess(moduleCode, role);
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

  openAddRole(): void {
    this.editingRoleId.set(null);
    const defaults = this.baseRoleOptions();
    this.roleForm = {
      name: '',
      code: '',
      description: '',
      baseRole: defaults.includes('EMPLOYEE') ? 'EMPLOYEE' : (defaults[0] ?? 'EMPLOYEE'),
    };
    this.roleFormError.set(null);
    this.orgRolesStore.error.set(null);
    this.showRoleModal.set(true);
  }

  openEditRole(role: OrganizationRoleRow): void {
    this.editingRoleId.set(role.id);
    this.roleForm = {
      name: role.name,
      code: role.code,
      description: role.description ?? '',
      baseRole: role.baseRole,
    };
    this.roleFormError.set(null);
    this.orgRolesStore.error.set(null);
    this.showRoleModal.set(true);
  }

  closeRoleModal(): void {
    this.showRoleModal.set(false);
    this.roleFormError.set(null);
  }

  async saveRole(): Promise<void> {
    if (!this.roleForm.name.trim()) {
      this.roleFormError.set('roles.customNameRequired');
      return;
    }
    if (!this.roleForm.baseRole) {
      this.roleFormError.set('roles.customBaseInvalid');
      return;
    }

    const id = this.editingRoleId();
    const ok =
      id == null
        ? await this.orgRolesStore.create({
            name: this.roleForm.name,
            code: this.roleForm.code.trim() || null,
            description: this.roleForm.description,
            baseRole: this.roleForm.baseRole,
          })
        : await this.orgRolesStore.update(id, {
            name: this.roleForm.name,
            description: this.roleForm.description,
            baseRole: this.roleForm.baseRole,
          });

    if (ok) this.closeRoleModal();
    else this.roleFormError.set(this.orgRolesStore.error());
  }

  async deleteRole(id: number): Promise<void> {
    await this.orgRolesStore.remove(id);
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
