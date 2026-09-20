import { Injectable, signal } from '@angular/core';
import { ProjectHealthAnalysisResponse } from '../models/ai-health-analysis.model';

/** Session cache so reopening the AI panel does not wait on the API again. */
@Injectable({ providedIn: 'root' })
export class ProjectAiAnalysisStore {
  private readonly byProject = signal<Record<number, ProjectHealthAnalysisResponse>>({});

  get(projectId: number): ProjectHealthAnalysisResponse | null {
    return this.byProject()[projectId] ?? null;
  }

  set(projectId: number, result: ProjectHealthAnalysisResponse): void {
    this.byProject.update((map) => ({ ...map, [projectId]: result }));
  }
}
