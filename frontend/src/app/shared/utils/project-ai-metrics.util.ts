import { ProjectRow, StageRow, TaskCard } from '../../core/data/mock-data';
import { ProjectHealthAnalysisRequest } from '../../core/models/ai-health-analysis.model';
import { ProjectRisk } from '../../core/models/risk.model';
import { ProjectHealthResult } from './project-health.util';

export function buildProjectHealthAnalysisRequest(
  project: ProjectRow,
  tasks: TaskCard[],
  risks: ProjectRisk[],
  stages: StageRow[],
  health: ProjectHealthResult | undefined,
): ProjectHealthAnalysisRequest {
  const today = new Date().toISOString().slice(0, 10);
  const completedTasks = tasks.filter((t) => t.status === 'DONE').length;
  const overdueTasks = tasks.filter((t) => t.dueDate < today && t.status !== 'DONE').length;
  const openRisks = risks.filter((r) => r.status !== 'CLOSED');
  const criticalRisks = openRisks.filter((r) => r.riskLevel === 'CRITICAL').length;

  const distribution = { low: 0, medium: 0, high: 0, critical: 0 };
  for (const risk of openRisks) {
    const key = risk.riskLevel.toLowerCase() as keyof typeof distribution;
    if (key in distribution) distribution[key]++;
  }

  const completedStages = stages.filter((s) => s.status === 'Done').length;
  const delayedStages = stages.filter((s) => s.endDate < today && s.status !== 'Done').length;
  const taskCompletionRate = tasks.length === 0 ? 0 : (completedTasks / tasks.length) * 100;

  return {
    project: {
      name: project.name,
      status: project.status,
      progressPercentage: project.progress,
      startDate: project.startDate || null,
      endDate: project.endDate || null,
    },
    tasks: {
      totalTasks: tasks.length,
      completedTasks,
      delayedTasks: overdueTasks,
      overdueTasks,
    },
    risks: {
      openRisks: openRisks.length,
      criticalRisks,
      severityDistribution: distribution,
    },
    stages: {
      totalStages: stages.length,
      completedStages: completedStages,
      delayedStages: delayedStages,
    },
    changeRequests: {
      openChangeRequests: 0,
      approvedChangeRequests: 0,
    },
    performance: {
      teamKpiScore: Math.round(taskCompletionRate),
      projectHealthScore: health?.score ?? project.progress,
    },
  };
}
