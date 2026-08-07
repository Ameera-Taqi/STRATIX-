import { Injectable, computed, inject } from '@angular/core';
import { ProjectsStore } from './projects.store';
import { TasksStore } from './tasks.store';
import { RisksStore } from './risks.store';
import { computeProjectHealth, ProjectHealthResult } from '../../shared/utils/project-health.util';

@Injectable({ providedIn: 'root' })
export class ProjectHealthService {
  private readonly projects = inject(ProjectsStore);
  private readonly tasks = inject(TasksStore);
  private readonly risks = inject(RisksStore);

  readonly all = computed((): ProjectHealthResult[] => {
    this.projects.projects();
    this.tasks.tasks();
    this.risks.risks();

    const today = new Date().toISOString().slice(0, 10);

    return this.projects.projects().map((project) => {
      const projectTasks = this.tasks.getByProject(project.id);
      const completedTasks = projectTasks.filter((t) => t.status === 'DONE');
      // Backend uses CompletedAt ≤ DueDate; without completedAt on the card, treat DONE as on-time.
      const onTimeCompletedTasks = completedTasks.length;
      const delayedTasks = projectTasks.filter(
        (t) => !!t.dueDate && t.dueDate < today && t.status !== 'DONE',
      ).length;
      const blockedTasks = projectTasks.filter((t) => t.status === 'BLOCKED').length;
      const criticalRisks = this.risks
        .getByProject(project.id)
        .filter((r) => r.riskLevel === 'CRITICAL' && r.status !== 'CLOSED').length;

      return computeProjectHealth({
        project,
        totalTasks: projectTasks.length,
        completedTasks: completedTasks.length,
        onTimeCompletedTasks,
        delayedTasks,
        blockedTasks,
        criticalRisks,
      });
    });
  });

  getByProjectId(projectId: number): ProjectHealthResult | undefined {
    const target = Number(projectId);
    return this.all().find((h) => Number(h.projectId) === target);
  }

  readonly portfolioScore = computed(() => {
    const list = this.all();
    if (list.length === 0) return { score: 0, status: 'CRITICAL' as const };
    const score = Math.round(list.reduce((sum, h) => sum + h.score, 0) / list.length);
    if (score >= 70) return { score, status: 'HEALTHY' as const };
    if (score >= 45) return { score, status: 'WARNING' as const };
    return { score, status: 'CRITICAL' as const };
  });
}
