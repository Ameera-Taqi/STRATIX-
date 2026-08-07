export interface ProjectHealthSnapshot {
  id: number;
  projectId: number;
  projectName: string;
  score: number;
  status: string;
  progress: number;
  onTimeTasks: number;
  delayedTasks: number;
  criticalRisks: number;
  noteKey?: string | null;
  capturedAt: string;
}
