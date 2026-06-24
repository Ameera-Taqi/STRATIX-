import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { map } from 'rxjs';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { EditEmployeeForm, EmployeeStatus, EmployeesStore } from '../../core/services/employees.store';
import { ProjectsStore } from '../../core/services/projects.store';
import { RoleAccessService } from '../../core/services/role-access.service';
import { TasksStore } from '../../core/services/tasks.store';
import { EmployeeStatusToggleComponent } from '../../shared/components/employee-status-toggle.component';
import { priorityClass } from '../../shared/utils/status.util';
import { departmentBadgeClass, employeeInitials } from '../../shared/utils/employee.util';
import { computeEmployeeTaskStats } from '../../shared/utils/employee-stats.util';
import { TranslatePipe } from '../../core/i18n/translate.pipe';

@Component({
  selector: 'app-employee-detail',
  standalone: true,
  imports: [TopbarComponent, RouterLink, TranslatePipe, FormsModule, EmployeeStatusToggleComponent],
  templateUrl: './employee-detail.component.html',
})
export class EmployeeDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly tasksStore = inject(TasksStore);
  private readonly projectsStore = inject(ProjectsStore);
  private readonly roleAccess = inject(RoleAccessService);

  private readonly employeeId = toSignal(
    this.route.paramMap.pipe(map((params) => Number(params.get('id')))),
    { initialValue: Number(this.route.snapshot.paramMap.get('id')) },
  );

  readonly showEditModal = signal(false);
  readonly formError = signal<string | null>(null);
  readonly submitting = signal(false);

  readonly departments = this.employeesStore.departmentOptions;
  readonly roleOptions = [
    'Project Manager',
    'Team Leader',
    'Employee',
    'Admin',
    'Executive Viewer',
  ];

  form: EditEmployeeForm = {
    name: '',
    email: '',
    role: 'Employee',
    department: 'IT',
    status: 'Active',
  };

  readonly canEdit = computed(() => {
    const role = this.roleAccess.role();
    return role === 'ADMIN' || role === 'PROJECT_MANAGER';
  });

  readonly priorityClass = priorityClass;

  readonly employee = computed(() => {
    const id = this.employeeId();
    this.employeesStore.employees();
    this.tasksStore.tasks();
    this.projectsStore.projects();
    const row = this.employeesStore.getById(id);
    if (!row) return undefined;
    return {
      ...row,
      ...computeEmployeeTaskStats(
        row.id,
        row.name,
        this.tasksStore.getAll(),
        this.projectsStore.projects(),
      ),
    };
  });

  readonly assignedTasks = computed(() => {
    this.tasksStore.tasks();
    const emp = this.employee();
    if (!emp) return [];
    return this.tasksStore.getByEmployee(emp.id, emp.name);
  });

  readonly initials = employeeInitials;
  readonly departmentBadge = departmentBadgeClass;

  openEditModal(): void {
    const emp = this.employee();
    if (!emp) return;
    this.formError.set(null);

    const openForm = (email: string) => {
      this.form = {
        name: emp.name,
        email,
        role: emp.role,
        department: emp.department,
        status: emp.status,
      };
      this.showEditModal.set(true);
    };

    if (emp.email?.trim()) {
      openForm(emp.email);
      return;
    }

    this.employeesStore.refreshUser(this.employeeId()).subscribe({
      next: (row) => openForm(row.email ?? ''),
      error: () => openForm(''),
    });
  }

  closeEditModal(): void {
    this.showEditModal.set(false);
    this.formError.set(null);
  }

  saveEmployee(): void {
    if (!this.form.name.trim()) {
      this.formError.set('team.errorName');
      return;
    }
    if (!this.form.email.trim()) {
      this.formError.set('team.errorEmail');
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);

    this.employeesStore.updateEmployee(this.employeeId(), this.form).subscribe({
      next: () => {
        this.submitting.set(false);
        this.closeEditModal();
        this.projectsStore.reloadUsers();
      },
      error: (err) => {
        this.submitting.set(false);
        this.formError.set(this.resolveSaveError(err));
      },
    });
  }

  setStatus(status: EmployeeStatus): void {
    if (!this.employee()) return;
    this.employeesStore.updateStatus(this.employeeId(), status).subscribe({
      error: () => this.formError.set('team.errorSave'),
    });
  }

  ngOnInit(): void {
    this.employeesStore.loadFromApi();
  }

  private resolveSaveError(err: unknown): string {
    if (!(err instanceof HttpErrorResponse)) return 'team.errorSave';
    const message = String(err.error?.message ?? err.error?.error ?? err.message ?? '').toLowerCase();
    if (message.includes('email already')) return 'team.errorEmailTaken';
    if (message.includes('department not found')) return 'team.errorDepartment';
    if (err.status === 403 || err.status === 401) return 'team.errorPermission';
    return 'team.errorSave';
  }
}
