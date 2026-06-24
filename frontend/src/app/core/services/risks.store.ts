import { Injectable, computed, inject, signal } from '@angular/core';

import {
  CreateRiskForm,
  ProjectRisk,
  RiskDashboardStats,
  RiskHeatMapData,
  RiskImpact,
  RiskProbability,
} from '../models/risk.model';

import { ApiService } from './api.service';

import { NotificationsStore } from './notifications.store';

import { calculateRiskLevel } from '../../shared/utils/risk.util';



@Injectable({ providedIn: 'root' })

export class RisksStore {

  private readonly api = inject(ApiService);

  private readonly notifications = inject(NotificationsStore);

  private readonly _risks = signal<ProjectRisk[]>([]);

  private readonly _loaded = signal(false);

  private _nextId = 100;



  readonly risks = this._risks.asReadonly();

  readonly loaded = this._loaded.asReadonly();



  readonly stats = computed<RiskDashboardStats>(() => {

    const list = this._risks();

    return {

      totalRisks: list.length,

      openRisks: list.filter((r) => r.status === 'OPEN').length,

      criticalRisks: list.filter((r) => r.riskLevel === 'CRITICAL').length,

      closedRisks: list.filter((r) => r.status === 'CLOSED').length,

    };

  });



  readonly heatMap = computed<RiskHeatMapData>(() => {

    const list = this._risks();

    const impacts: RiskImpact[] = ['LOW', 'MEDIUM', 'HIGH'];

    const probabilities: RiskProbability[] = ['LOW', 'MEDIUM', 'HIGH'];

    const cells = impacts.flatMap((impact) =>

      probabilities.map((probability) => ({

        impact,

        probability,

        riskLevel: calculateRiskLevel(impact, probability),

        count: list.filter((r) => r.impact === impact && r.probability === probability).length,

      })),

    );

    return { cells, totalRisks: list.length };

  });



  loadFromApi(): void {

    this.api.getRisks().subscribe({

      next: (risks) => {
        this._risks.set(this.sortByNewest(risks));
        this._loaded.set(true);
        if (risks.length > 0) {
          const maxId = Math.max(...risks.map((r) => r.id), this._nextId);
          this._nextId = maxId + 1;
        }
      },

      error: () => this.loadOpenAndCriticalFallback(),

    });

  }



  private loadOpenAndCriticalFallback(): void {

    this.api.getRisksOpen().subscribe({

      next: (openRisks) => {

        this.api.getRisksCritical().subscribe({

          next: (criticalRisks) => {

            const merged = this.mergeUnique([...openRisks, ...criticalRisks]);
            this._risks.set(this.sortByNewest(merged));
            this._loaded.set(true);

          },

          error: () => this._loaded.set(true),

        });

      },

      error: () => this._loaded.set(true),

    });

  }



  /** Merge API risks with local-only entries (e.g. newly created before sync). */

  private mergeApiRisks(fromApi: ProjectRisk[]): void {

    if (fromApi.length === 0) return;

    const apiIds = new Set(fromApi.map((r) => r.id));

    const localOnly = this._risks().filter((r) => !apiIds.has(r.id));

    this._risks.set(this.sortByNewest(this.mergeUnique([...fromApi, ...localOnly])));

    const maxId = Math.max(...this._risks().map((r) => r.id), this._nextId);

    this._nextId = maxId + 1;

  }



  private sortByNewest(risks: ProjectRisk[]): ProjectRisk[] {

    return [...risks].sort((a, b) => b.createdAt.localeCompare(a.createdAt));

  }



  getById(id: number): ProjectRisk | undefined {

    return this._risks().find((r) => r.id === id);

  }



  getByProject(projectId: number): ProjectRisk[] {

    return this._risks().filter((r) => r.projectId === projectId);

  }



  getByOwner(ownerId: number): ProjectRisk[] {

    return this._risks().filter((r) => r.ownerId === ownerId);

  }



  getOpen(): ProjectRisk[] {

    return this._risks().filter((r) => r.status === 'OPEN');

  }



  getCritical(): ProjectRisk[] {

    return this._risks().filter((r) => r.riskLevel === 'CRITICAL');

  }



  addRisk(form: CreateRiskForm, projectName: string, ownerName: string): ProjectRisk {

    const riskLevel = calculateRiskLevel(form.impact, form.probability);

    const tempId = ++this._nextId;

    const risk: ProjectRisk = {

      id: tempId,

      title: form.title.trim(),

      description: form.description.trim(),

      impact: form.impact,

      probability: form.probability,

      riskLevel,

      mitigationPlan: form.mitigationPlan.trim(),

      status: form.status,

      projectId: form.projectId,

      projectName,

      ownerId: form.ownerId,

      ownerName,

      createdAt: new Date().toISOString(),

      updatedAt: new Date().toISOString(),

    };

    this._risks.update((list) => [risk, ...list]);

    this.logRiskCreated(risk, projectName);



    this.api.createRisk(form).subscribe({

      next: (created) => {

        this._risks.update((list) => list.map((r) => (r.id === tempId ? created : r)));

        if (created.id >= this._nextId) this._nextId = created.id + 1;

      },

      error: () => {

        /* keep optimistic local entry when API is unavailable */

      },

    });



    return risk;

  }



  updateRisk(id: number, form: CreateRiskForm, projectName: string, ownerName: string): void {

    const riskLevel = calculateRiskLevel(form.impact, form.probability);

    this._risks.update((list) =>

      list.map((r) =>

        r.id === id

          ? {

              ...r,

              title: form.title.trim(),

              description: form.description.trim(),

              impact: form.impact,

              probability: form.probability,

              riskLevel,

              mitigationPlan: form.mitigationPlan.trim(),

              status: form.status,

              projectId: form.projectId,

              projectName,

              ownerId: form.ownerId,

              ownerName,

              updatedAt: new Date().toISOString(),

            }

          : r,

      ),

    );

    this.api.updateRisk(id, form).subscribe({

      next: (updated) => {

        this._risks.update((list) => list.map((r) => (r.id === id ? updated : r)));

      },

      error: () => {

        /* local state already updated */

      },

    });

  }



  deleteRisk(id: number): void {

    this._risks.update((list) => list.filter((r) => r.id !== id));

    this.api.deleteRisk(id).subscribe({ error: () => {} });

  }



  private logRiskCreated(risk: ProjectRisk, projectName: string): void {

    this.notifications.push({

      titleKey: 'notifications.riskCreatedTitle',

      bodyKey: 'notifications.riskCreatedBody',

      params: { risk: risk.title },

    });

  }



  private mergeUnique(risks: ProjectRisk[]): ProjectRisk[] {

    const map = new Map<number, ProjectRisk>();

    for (const risk of risks) {

      map.set(risk.id, risk);

    }

    return [...map.values()];

  }

}


