export interface ProjectHealthAnalysisRequest {
  projectId: number;
}

export interface ProjectHealthAnalysisResponse {
  healthStatus: 'HEALTHY' | 'WARNING' | 'CRITICAL';
  deliveryRisk: 'LOW' | 'MEDIUM' | 'HIGH';
  executiveSummary: string;
  mainConcerns: string[];
  recommendations: string[];
  managementInsights: string[];
  analysisEngine?: 'OPENAI' | 'STRATIX_ENGINE';
  /** ISO timestamp — analysis is a snapshot of project data at this moment. */
  analyzedAt?: string;
}
