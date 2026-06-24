export interface ProjectHealthAnalysisRequest {
  project: {
    name: string;
    status: string;
    progressPercentage: number;
    startDate: string | null;
    endDate: string | null;
  };
  tasks: {
    totalTasks: number;
    completedTasks: number;
    delayedTasks: number;
    overdueTasks: number;
  };
  risks: {
    openRisks: number;
    criticalRisks: number;
    severityDistribution: {
      low: number;
      medium: number;
      high: number;
      critical: number;
    };
  };
  stages: {
    totalStages: number;
    completedStages: number;
    delayedStages: number;
  };
  changeRequests: {
    openChangeRequests: number;
    approvedChangeRequests: number;
  };
  performance: {
    teamKpiScore: number;
    projectHealthScore: number;
  };
}

export interface ProjectHealthAnalysisResponse {
  healthStatus: 'HEALTHY' | 'WARNING' | 'CRITICAL';
  deliveryRisk: 'LOW' | 'MEDIUM' | 'HIGH';
  executiveSummary: string;
  mainConcerns: string[];
  recommendations: string[];
  managementInsights: string[];
  analysisEngine?: 'OPENAI' | 'STRATIX_ENGINE';
}
