export type OrganizationStatus = 'ACTIVE' | 'SUSPENDED' | 'CANCELLED';
export type SubscriptionPlanCode = 'FREE' | 'PRO' | 'ENTERPRISE';

export interface OrganizationRow {
  id: number;
  name: string;
  slug: string;
  status: OrganizationStatus | string;
  subscriptionPlan: SubscriptionPlanCode | string;
  createdAt: string;
  userCount: number;
  projectCount: number;
}

export interface PlanRow {
  id: number;
  name: string;
  maxUsers: number;
  maxProjects: number;
  aiEnabled: boolean;
  storageLimitMb: number;
  price: number;
}

export interface UpdateOrganizationRequest {
  status?: OrganizationStatus | string;
  subscriptionPlan?: SubscriptionPlanCode | string;
}

export interface CreateOrganizationRequest {
  organizationName: string;
  adminName: string;
  adminEmail: string;
  password: string;
  slug?: string | null;
  subscriptionPlan?: SubscriptionPlanCode | string | null;
}
