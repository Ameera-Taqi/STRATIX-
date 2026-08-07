import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, catchError, map, tap, throwError } from 'rxjs';
import { EmployeeRow, EmployeeStatus, ProjectRow, TaskCard } from '../data/mock-data';
import { User, UserDirectoryItem, UserRole, UserStatus } from '../models/user.model';
import { ApiService } from './api.service';
import { computeEmployeeTaskStats } from '../../shared/utils/employee-stats.util';

export type { EmployeeStatus };

export interface NewEmployeeForm {
  name: string;
  role: UserRole;
  department: string;
  status: EmployeeStatus;
  email?: string;
}

export interface EditEmployeeForm {
  name: string;
  email: string;
  role: UserRole;
  department: string;
  status: EmployeeStatus;
}

export const DEFAULT_EMPLOYEE_PASSWORD = 'stratix123';

@Injectable({ providedIn: 'root' })
export class EmployeesStore {
  private readonly api = inject(ApiService);

  private readonly _employees = signal<EmployeeRow[]>([]);
  private readonly _departmentsByName = signal<Record<string, number>>({});
  private readonly _loaded = signal(false);
  private _lastTasks: TaskCard[] = [];
  private _lastProjects: ProjectRow[] = [];

  readonly employees = this._employees.asReadonly();
  readonly loaded = this._loaded.asReadonly();
  readonly departmentOptions = computed(() => {
    const names = Object.keys(this._departmentsByName());
    return names.length > 0
      ? names.sort()
      : ['IT', 'Product', 'Operations', 'HR', 'Finance'];
  });

  /** Call after departments CRUD so employee forms stay in sync. */
  reloadDepartments(): void {
    this.api.getDepartments().subscribe({
      next: (departments) => {
        const map: Record<string, number> = {};
        for (const dept of departments) {
          map[dept.name] = dept.id;
        }
        this._departmentsByName.set(map);
      },
    });
  }
  /** Active users for pickers / bootstrap (directory API — all tenant roles). */
  loadFromApi(onReady?: () => void): void {
    this.api.getDepartments().subscribe({
      next: (departments) => {
        const map: Record<string, number> = {};
        for (const dept of departments) {
          map[dept.name] = dept.id;
        }
        this._departmentsByName.set(map);
      },
    });

    this.api.getDirectoryUsers().subscribe({
      next: (users) => {
        this._employees.set(users.map((u) => this.fromUser(u)));
        this._loaded.set(true);
        this.recomputeStats();
        onReady?.();
      },
      error: () => {
        this._loaded.set(true);
        onReady?.();
      },
    });
  }

  /** Full user list for Team administration (OrgAdmins only). */
  loadAdministrationFromApi(onReady?: () => void): void {
    this.api.getDepartments().subscribe({
      next: (departments) => {
        const map: Record<string, number> = {};
        for (const dept of departments) {
          map[dept.name] = dept.id;
        }
        this._departmentsByName.set(map);
      },
    });

    this.api.getUsers().subscribe({
      next: (users) => {
        this._employees.set(users.map((u) => this.fromUser(u)));
        this._loaded.set(true);
        this.recomputeStats();
        onReady?.();
      },
      error: () => {
        this._loaded.set(true);
        onReady?.();
      },
    });
  }

  getAll(): EmployeeRow[] {
    return this._employees();
  }

  getById(id: number): EmployeeRow | undefined {
    return this._employees().find((e) => e.id === id);
  }

  refreshUser(id: number): Observable<EmployeeRow> {
    return this.api.getUser(id).pipe(
      map((user) => this.mergeUserIntoRow(user, this.getById(id))),
      tap((row) => {
        this._employees.update((list) => list.map((e) => (e.id === id ? row : e)));
        this.recomputeStats();
      }),
    );
  }

  addEmployee(form: NewEmployeeForm): Observable<EmployeeRow> {
    const email = (form.email?.trim() || this.suggestEmail(form.name)).toLowerCase();
    const tempId = -Date.now();
    const optimistic: EmployeeRow = {
      id: tempId,
      name: form.name.trim(),
      role: form.role,
      department: form.department,
      projects: 0,
      completedTasks: 0,
      status: form.status,
      tasksCompleted: 0,
      delayedTasks: 0,
      onTimePct: 100,
      kpiScore: 0,
    };

    this._employees.update((list) => [...list, optimistic]);

    const body = {
      name: form.name.trim(),
      email,
      password: DEFAULT_EMPLOYEE_PASSWORD,
      role: this.toRoleCode(form.role),
      jobTitle: form.role,
      status: this.toUserStatus(form.status),
      departmentId: this.resolveDepartmentId(form.department),
    };

    return this.api.createUser(body).pipe(
      map((user) => this.fromUser(user)),
      tap((row) => {
        this._employees.update((list) =>
          list.map((e) => (e.id === tempId ? row : e)),
        );
      }),
      catchError((err) => {
        this._employees.update((list) => list.filter((e) => e.id !== tempId));
        return throwError(() => err);
      }),
    );
  }

  updateStatus(id: number, status: EmployeeStatus): Observable<EmployeeRow> {
    const employee = this.getById(id);
    if (!employee) {
      return throwError(() => new Error('Employee not found'));
    }
    return this.updateEmployee(id, {
      name: employee.name,
      email: employee.email ?? this.suggestEmail(employee.name),
      role: this.normalizeRoleCode(employee.role),
      department: employee.department,
      status,
    });
  }

  updateEmployee(id: number, form: EditEmployeeForm): Observable<EmployeeRow> {
    const previous = this.getById(id);
    const optimistic: EmployeeRow | undefined = previous
      ? {
          ...previous,
          name: form.name.trim(),
          email: form.email.trim().toLowerCase(),
          role: form.role,
          department: form.department,
          status: form.status,
        }
      : undefined;

    if (optimistic) {
      this._employees.update((list) => list.map((e) => (e.id === id ? optimistic : e)));
    }

    const body = {
      name: form.name.trim(),
      email: form.email.trim().toLowerCase(),
      role: this.toRoleCode(form.role),
      jobTitle: form.role,
      status: this.toUserStatus(form.status),
      departmentId: this.resolveDepartmentId(form.department),
    };

    return this.api.updateUser(id, body).pipe(
      map((user) => this.mergeUserIntoRow(user, previous)),
      tap((row) => {
        this._employees.update((list) => list.map((e) => (e.id === id ? row : e)));
        this.recomputeStats();
      }),
      catchError((err) => {
        if (previous) {
          this._employees.update((list) => list.map((e) => (e.id === id ? previous : e)));
        }
        return throwError(() => err);
      }),
    );
  }

  syncFromTasks(tasks: TaskCard[]): void {
    this._lastTasks = tasks;
    this.recomputeStats();
  }

  syncFromProjects(projects: ProjectRow[]): void {
    this._lastProjects = projects;
    this.recomputeStats();
  }

  private recomputeStats(): void {
    if (this._employees().length === 0) return;

    this._employees.update((list) =>
      list.map((employee) => {
        const stats = computeEmployeeTaskStats(
          employee.id,
          employee.name,
          this._lastTasks,
          this._lastProjects,
        );
        return { ...employee, ...stats };
      }),
    );
  }

  private fromUser(user: User | UserDirectoryItem): EmployeeRow {
    return {
      id: user.id,
      name: user.name,
      email: user.email,
      role: this.normalizeRoleCode(user.role),
      department: user.departmentName ?? '',
      projects: 0,
      completedTasks: 0,
      status: user.status === 'ACTIVE' ? 'Active' : 'Inactive',
      tasksCompleted: 0,
      delayedTasks: 0,
      onTimePct: 100,
      kpiScore: 0,
    };
  }

  private mergeUserIntoRow(user: User, previous?: EmployeeRow): EmployeeRow {
    const base = this.fromUser(user);
    if (!previous) return base;
    return {
      ...base,
      projects: previous.projects,
      completedTasks: previous.completedTasks,
      tasksCompleted: previous.tasksCompleted,
      delayedTasks: previous.delayedTasks,
      onTimePct: previous.onTimePct,
      kpiScore: previous.kpiScore,
    };
  }

  private suggestEmail(name: string): string {
    const slug = name
      .trim()
      .toLowerCase()
      .replace(/\s+/g, '.')
      .replace(/[^a-z0-9.]/g, '');
    const base = slug || 'employee';
    return `${base}@stratix.local`;
  }

  private resolveDepartmentId(name: string): number | null {
    return this._departmentsByName()[name] ?? null;
  }

  private toRoleCode(role: string): UserRole {
    return this.normalizeRoleCode(role);
  }

  private normalizeRoleCode(role: string): UserRole {
    const upper = role.trim().toUpperCase().replace(/\s+/g, '_');
    const codes: UserRole[] = [
      'SUPER_ADMIN',
      'ORG_ADMIN',
      'ADMIN',
      'PROJECT_MANAGER',
      'TEAM_LEADER',
      'EMPLOYEE',
      'EXECUTIVE_VIEWER',
    ];
    if (codes.includes(upper as UserRole)) return upper as UserRole;

    const map: Record<string, UserRole> = {
      'Super Admin': 'SUPER_ADMIN',
      'Org Admin': 'ORG_ADMIN',
      'Organization Admin': 'ORG_ADMIN',
      Admin: 'ADMIN',
      'Project Manager': 'PROJECT_MANAGER',
      'Team Leader': 'TEAM_LEADER',
      Employee: 'EMPLOYEE',
      'Executive Viewer': 'EXECUTIVE_VIEWER',
    };
    return map[role] ?? 'EMPLOYEE';
  }

  private toUserStatus(status: EmployeeStatus): UserStatus {
    return status === 'Active' ? 'ACTIVE' : 'INACTIVE';
  }
}
