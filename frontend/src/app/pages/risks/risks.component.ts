import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { TopbarComponent } from '../../layout/topbar/topbar.component';
import { TranslatePipe } from '../../core/i18n/translate.pipe';
import { RisksStore } from '../../core/services/risks.store';
import { ProjectsStore } from '../../core/services/projects.store';
import { EmployeesStore } from '../../core/services/employees.store';
import { RoleAccessService } from '../../core/services/role-access.service';
import { CurrentUserService } from '../../core/services/current-user.service';
import {
  CreateRiskForm,
  RiskImpact,
  RiskProbability,
  RiskStatus,
} from '../../core/models/risk.model';
import { riskLevelClass, riskStatusClass } from '../../shared/utils/risk.util';

@Component({
  selector: 'app-risks',
  standalone: true,
  imports: [TopbarComponent, RouterLink, TranslatePipe, FormsModule],
  templateUrl: './risks.component.html',
  styles: `
    :host {
      display: flex;
      flex: 1 1 auto;
      flex-direction: column;
      min-height: 0;
      overflow: hidden;
    }
  `,
})
export class RisksComponent implements OnInit {
  private readonly store = inject(RisksStore);
  private readonly projectsStore = inject(ProjectsStore);
  private readonly employeesStore = inject(EmployeesStore);
  private readonly roleAccess = inject(RoleAccessService);
  private readonly currentUser = inject(CurrentUserService);
  private readonly router = inject(Router);

  readonly risks = this.store.risks;
  readonly stats = this.store.stats;
  readonly heatMap = this.store.heatMap;
  readonly canWrite = computed(() => this.roleAccess.canWrite('RISKS'));
  readonly canDelete = computed(() => this.roleAccess.role() === 'ADMIN');

  readonly search = signal('');
  readonly statusFilter = signal<'ALL' | RiskStatus>('ALL');
  readonly levelFilter = signal<'ALL' | 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW'>('ALL');
  readonly showModal = signal(false);
  readonly editingId = signal<number | null>(null);
  readonly formError = signal<string | null>(null);

  readonly riskLevelClass = riskLevelClass;
  readonly riskStatusClass = riskStatusClass;

  readonly impactOptions: RiskImpact[] = ['LOW', 'MEDIUM', 'HIGH'];
  readonly probabilityOptions: RiskProbability[] = ['LOW', 'MEDIUM', 'HIGH'];
  readonly statusOptions: RiskStatus[] = ['OPEN', 'MITIGATING', 'CLOSED'];

  readonly projects = this.projectsStore.projects;
  readonly employees = this.employeesStore.employees;

  form: CreateRiskForm = {
    title: '',
    description: '',
    impact: 'MEDIUM',
    probability: 'MEDIUM',
    mitigationPlan: '',
    status: 'OPEN',
    projectId: 1,
    ownerId: 1,
  };

  getHeatMapCell(impact: RiskImpact, probability: RiskProbability) {
    return this.heatMap().cells.find((c) => c.impact === impact && c.probability === probability);
  }

  filtered = computed(() => {
    const q = this.search().toLowerCase();
    const status = this.statusFilter();
    const level = this.levelFilter();
    return this.risks().filter((r) => {
      const matchSearch =
        !q ||
        r.title.toLowerCase().includes(q) ||
        r.projectName.toLowerCase().includes(q) ||
        r.ownerName.toLowerCase().includes(q);
      const matchStatus = status === 'ALL' || r.status === status;
      const matchLevel = level === 'ALL' || r.riskLevel === level;
      return matchSearch && matchStatus && matchLevel;
    });
  });

  ngOnInit(): void {
    this.form = this.emptyForm();
    this.store.loadFromApi();
  }

  modalTitle(): string {
    return this.editingId() ? 'risks.editTitle' : 'risks.createTitle';
  }

  openCreate(): void {
    if (!this.canWrite()) return;
    this.editingId.set(null);
    this.formError.set(null);
    this.form = this.emptyForm();
    this.showModal.set(true);
  }

  openEdit(id: number): void {
    if (!this.canWrite()) return;
    const risk = this.store.getById(id);
    if (!risk) return;
    this.editingId.set(id);
    this.formError.set(null);
    this.form = {
      title: risk.title,
      description: risk.description,
      impact: risk.impact,
      probability: risk.probability,
      mitigationPlan: risk.mitigationPlan,
      status: risk.status,
      projectId: risk.projectId,
      ownerId: risk.ownerId,
    };
    this.showModal.set(true);
  }

  closeModal(): void {
    this.showModal.set(false);
    this.editingId.set(null);
    this.formError.set(null);
  }

  saveRisk(): void {
    if (!this.form.title.trim()) {
      this.formError.set('risks.errorTitle');
      return;
    }
    const project = this.projects().find((p) => p.id === this.form.projectId);
    const owner = this.employees().find((e) => e.id === this.form.ownerId);
    if (!project || !owner) {
      this.formError.set('risks.errorRelations');
      return;
    }

    const editing = this.editingId();
    if (editing) {
      this.store.updateRisk(editing, this.form, project.name, owner.name);
      this.closeModal();
      void this.router.navigate(['/risks', editing]);
      return;
    }

    this.store.addRisk(this.form, project.name, owner.name);
    this.closeModal();
  }

  deleteRisk(id: number): void {
    if (!this.canDelete()) return;
    this.store.deleteRisk(id);
  }

  private emptyForm(): CreateRiskForm {
    const profile = this.currentUser.profile();
    return {
      title: '',
      description: '',
      impact: 'MEDIUM',
      probability: 'MEDIUM',
      mitigationPlan: '',
      status: 'OPEN',
      projectId: this.projects()[0]?.id ?? 1,
      ownerId: profile.employeeId,
    };
  }
}
