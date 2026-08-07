export type RiskImpact = 'LOW' | 'MEDIUM' | 'HIGH';
export type RiskProbability = 'LOW' | 'MEDIUM' | 'HIGH';
export type RiskLevel = 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
export type RiskStatus = 'OPEN' | 'MITIGATING' | 'CLOSED';

export interface ProjectRisk {
  id: number;
  title: string;
  description: string;
  impact: RiskImpact;
  probability: RiskProbability;
  riskLevel: RiskLevel;
  mitigationPlan: string;
  status: RiskStatus;
  projectId: number;
  projectName: string;
  ownerId: number;
  ownerName: string;
  createdAt: string;
  updatedAt: string;
  closureReason?: string | null;
  residualRisk?: RiskImpact | null;
}

export interface RiskDashboardStats {
  totalRisks: number;
  openRisks: number;
  criticalRisks: number;
  closedRisks: number;
}

export interface RiskHeatMapCell {
  impact: RiskImpact;
  probability: RiskProbability;
  riskLevel: RiskLevel;
  count: number;
}

export interface RiskHeatMapData {
  cells: RiskHeatMapCell[];
  totalRisks: number;
}

export interface CreateRiskForm {
  title: string;
  description: string;
  impact: RiskImpact;
  probability: RiskProbability;
  mitigationPlan: string;
  status: RiskStatus;
  projectId: number;
  ownerId: number;
}

export type CreateRiskRequest = CreateRiskForm;

export interface UpdateRiskRequest {
  title: string;
  description: string;
  impact: RiskImpact;
  probability: RiskProbability;
  mitigationPlan: string;
  status: RiskStatus;
  projectId: number;
  ownerId: number;
}

export interface CloseRiskRequest {
  closureReason: string;
  residualRisk?: RiskImpact | null;
}
