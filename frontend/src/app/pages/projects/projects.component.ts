import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { LanguageService } from '../../core/i18n/language.service';
import { ProjectsStore } from '../../core/services/projects.store';
import { DepartmentsStore } from '../../core/services/departments.store';
import { ProjectHealthService } from '../../core/services/project-health.service';
import { ProjectHealthScoreComponent } from '../../shared/components/project-health-score/project-health-score.component';
import { RoleAccessService } from '../../core/services/role-access.service';
import { ProjectRow } from '../../core/data/mock-data';
import { priorityLabelKey, projectStatusLabelKey } from '../../shared/utils/enum-labels';
import { WarnUnsavedDirective } from '../../shared/directives/warn-unsaved.directive';
import {
  allowLeaveIfClean,
  formSnapshot,
  HasUnsavedChanges,
  isFormDirty,
} from '../../core/unsaved/unsaved-changes';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [
    TopbarComponent,
    StatusBadgeComponent,
    RouterLink,
    TranslatePipe,
    FormsModule,
    ProjectHealthScoreComponent,
    WarnUnsavedDirective,
  ],
  templateUrl: './projects.component.html',
})
export class ProjectsComponent implements OnInit, HasUnsavedChanges {
  readonly store = inject(ProjectsStore);
  private readonly departmentsStore = inject(DepartmentsStore);
  private readonly healthService = inject(ProjectHealthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly roleAccess = inject(RoleAccessService);
  private readonly lang = inject(LanguageService);
  private createFormBaseline: string | null = null;

  readonly projects = this.store.projects;
  readonly loading = this.store.loading;
  readonly loadError = this.store.loadError;
  readonly statusFilter = signal('All');
  readonly search = signal('');
  readonly showCreateModal = signal(false);
  readonly showDeleteModal = signal(false);
  readonly projectPendingDelete = signal<ProjectRow | null>(null);
  readonly formError = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly deletingId = signal<number | null>(null);
  readonly statusLabelKey = projectStatusLabelKey;
  readonly priorityLabelKeyFn = priorityLabelKey;

  readonly canDelete = computed(() => {
    const role = this.roleAccess.role();
    return role === 'ADMIN' || role === 'ORG_ADMIN' || role === 'PROJECT_MANAGER';
  });

  readonly canCreate = computed(() => this.roleAccess.canWrite('PROJECTS'));

  readonly departmentOptions = computed(() => this.departmentsStore.departments());
  readonly priorityOptions = ['LOW', 'MEDIUM', 'HIGH', 'URGENT'];

  readonly managerOptions = computed(() =>
    this.store.users().map((u) => ({
      id: u.id,
      name: u.name,
      department: u.departmentName ?? '',
    })),
  );

  readonly filtered = computed(() => {
    const status = this.statusFilter();
    const q = this.search().toLowerCase();
    return this.projects().filter((p) => {
      const matchStatus = status === 'All' || p.status.toLowerCase() === status.toLowerCase();
      const matchSearch = !q || p.name.toLowerCase().includes(q);
      return matchStatus && matchSearch;
    });
  });

  form = {
    name: '',
    description: '',
    department: '',
    managerId: null as number | null,
    startDate: '',
    endDate: '',
    priority: 'MEDIUM',
  };

  healthFor(projectId: number) {
    return this.healthService.getByProjectId(projectId);
  }

  ngOnInit(): void {
    this.store.loadFromApi();
    void this.departmentsStore.load();
    const q = this.route.snapshot.queryParamMap.get('q');
    if (q) {
      this.search.set(q);
    }
    if (this.route.snapshot.queryParamMap.get('create') === '1' && this.canCreate()) {
      this.openCreateModal();
      void this.router.navigate([], {
        relativeTo: this.route,
        queryParams: { create: null },
        queryParamsHandling: 'merge',
        replaceUrl: true,
      });
    }
  }

  hasUnsavedChanges(): boolean {
    return this.showCreateModal() && isFormDirty(this.form, this.createFormBaseline);
  }

  openCreateModal(): void {
    this.formError.set(null);
    const managers = this.managerOptions();
    const depts = this.departmentOptions();
    this.form = {
      name: '',
      description: '',
      department: depts[0]?.name ?? '',
      managerId: managers[0]?.id ?? null,
      startDate: new Date().toISOString().slice(0, 10),
      endDate: '',
      priority: 'MEDIUM',
    };
    this.createFormBaseline = formSnapshot(this.form);
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    if (!allowLeaveIfClean(this.hasUnsavedChanges(), this.lang)) return;
    this.showCreateModal.set(false);
    this.formError.set(null);
    this.createFormBaseline = null;
  }

  saveProject(): void {
    if (!this.form.name.trim()) {
      this.formError.set('projects.errorName');
      return;
    }
    if (!this.form.department.trim()) {
      this.formError.set('projects.errorDepartment');
      return;
    }
    if (this.form.managerId == null) {
      this.formError.set('projects.errorManager');
      return;
    }
    if (this.form.startDate && this.form.endDate && this.form.endDate < this.form.startDate) {
      this.formError.set('projects.errorDates');
      return;
    }

    const manager = this.managerOptions().find((m) => m.id === this.form.managerId);
    if (!manager) {
      this.formError.set('projects.errorManager');
      return;
    }

    this.submitting.set(true);
    this.formError.set(null);

    this.store
      .addProject({
        name: this.form.name,
        description: this.form.description,
        department: this.form.department,
        manager: manager.name,
        managerId: this.form.managerId,
        startDate: this.form.startDate,
        endDate: this.form.endDate,
        status: 'ACTIVE',
        priority: this.form.priority,
      })
      .subscribe({
        next: (project) => {
          this.submitting.set(false);
          this.createFormBaseline = null;
          this.showCreateModal.set(false);
          this.formError.set(null);
          // Land in the project workspace, not the portfolio list.
          void this.router.navigate(['/projects', project.id]);
        },
        error: () => {
          this.submitting.set(false);
          this.formError.set('projects.errorSave');
        },
      });
  }

  openDeleteModal(project: ProjectRow): void {
    if (!this.canDelete()) return;
    this.formError.set(null);
    this.projectPendingDelete.set(project);
    this.showDeleteModal.set(true);
  }

  closeDeleteModal(): void {
    if (this.deletingId() != null) return;
    this.showDeleteModal.set(false);
    this.projectPendingDelete.set(null);
  }

  confirmDeleteProject(): void {
    const project = this.projectPendingDelete();
    if (!project || !this.canDelete()) return;

    this.deletingId.set(project.id);
    this.store.deleteProject(project.id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.showDeleteModal.set(false);
        this.projectPendingDelete.set(null);
      },
      error: () => {
        this.deletingId.set(null);
        this.formError.set('projects.errorDelete');
      },
    });
  }
}
