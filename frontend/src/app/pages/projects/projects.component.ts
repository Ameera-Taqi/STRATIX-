import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { StatusBadgeComponent } from '../../shared/components/status-badge.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { ProjectsStore } from '../../core/services/projects.store';
import { ProjectHealthService } from '../../core/services/project-health.service';
import { ProjectHealthScoreComponent } from '../../shared/components/project-health-score/project-health-score.component';
import { RoleAccessService } from '../../core/services/role-access.service';
import { LanguageService } from '../../core/i18n/language.service';
import { ProjectRow } from '../../core/data/mock-data';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [TopbarComponent, StatusBadgeComponent, RouterLink, TranslatePipe, FormsModule, ProjectHealthScoreComponent],
  templateUrl: './projects.component.html',
})
export class ProjectsComponent implements OnInit {
  private readonly store = inject(ProjectsStore);
  private readonly healthService = inject(ProjectHealthService);
  private readonly router = inject(Router);
  private readonly roleAccess = inject(RoleAccessService);
  private readonly lang = inject(LanguageService);

  readonly projects = this.store.projects;
  readonly statusFilter = signal('All');
  readonly search = signal('');
  readonly showCreateModal = signal(false);
  readonly formError = signal<string | null>(null);
  readonly submitting = signal(false);
  readonly deletingId = signal<number | null>(null);

  readonly canDelete = computed(() => {
    const role = this.roleAccess.role();
    return role === 'ADMIN' || role === 'PROJECT_MANAGER';
  });

  readonly departments = ['IT', 'Product', 'Operations', 'HR', 'Finance'];
  readonly statusOptions = ['Planned', 'Active', 'On Hold'];
  readonly priorityOptions = ['LOW', 'MEDIUM', 'HIGH', 'CRITICAL'];

  readonly managerOptions = computed(() =>
    this.store.users().map((u) => ({
      id: u.id,
      name: u.name,
      department: u.departmentName ?? '',
    })),
  );

  form = {
    name: '',
    department: 'IT',
    managerId: null as number | null,
    startDate: '',
    endDate: '',
    status: 'Planned',
    priority: 'MEDIUM',
  };

  filtered() {
    return this.projects().filter((p) => {
      const matchStatus =
        this.statusFilter() === 'All' || p.status.toLowerCase() === this.statusFilter().toLowerCase();
      const q = this.search().toLowerCase();
      const matchSearch = !q || p.name.toLowerCase().includes(q);
      return matchStatus && matchSearch;
    });
  }

  healthFor(projectId: number) {
    return this.healthService.getByProjectId(projectId);
  }

  ngOnInit(): void {
    if (!this.store.loaded()) {
      this.store.loadFromApi();
    }
  }

  openCreateModal(): void {
    this.formError.set(null);
    const managers = this.managerOptions();
    this.form = {
      name: '',
      department: 'IT',
      managerId: managers[0]?.id ?? null,
      startDate: new Date().toISOString().slice(0, 10),
      endDate: '',
      status: 'Planned',
      priority: 'MEDIUM',
    };
    this.showCreateModal.set(true);
  }

  closeCreateModal(): void {
    this.showCreateModal.set(false);
    this.formError.set(null);
  }

  saveProject(): void {
    if (!this.form.name.trim()) {
      this.formError.set('projects.errorName');
      return;
    }
    if (this.form.managerId == null) {
      this.formError.set('projects.errorManager');
      return;
    }
    if (!this.form.endDate) {
      this.formError.set('projects.errorEndDate');
      return;
    }
    if (this.form.endDate < this.form.startDate) {
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
        department: this.form.department,
        manager: manager.name,
        managerId: this.form.managerId,
        startDate: this.form.startDate,
        endDate: this.form.endDate,
        status: this.form.status,
        priority: this.form.priority,
      })
      .subscribe({
        next: (project) => {
          this.submitting.set(false);
          this.closeCreateModal();
          this.router.navigate(['/projects', project.id]);
        },
        error: () => {
          this.submitting.set(false);
          this.formError.set('projects.errorSave');
        },
      });
  }

  deleteProject(project: ProjectRow): void {
    if (!this.canDelete()) return;

    const message = this.lang
      .t('projects.confirmDelete')
      .replace('{{name}}', project.name);
    if (!confirm(message)) return;

    this.deletingId.set(project.id);
    this.store.deleteProject(project.id).subscribe({
      next: () => this.deletingId.set(null),
      error: () => {
        this.deletingId.set(null);
        this.formError.set('projects.errorDelete');
      },
    });
  }
}
