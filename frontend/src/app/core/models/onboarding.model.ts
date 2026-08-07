export interface OrganizationOnboardingStatus {
  organizationId: number;
  organizationName: string;
  logoUrl: string | null;
  canUploadLogo: boolean;
  industry: string | null;
  timezone: string | null;
  preferredLanguage: string | null;
  onboardingCompleted: boolean;
  hasDepartment: boolean;
  hasTeamMember: boolean;
  hasProject: boolean;
}

export interface UpdateOrganizationProfileRequest {
  organizationName?: string | null;
  industry?: string | null;
  timezone?: string | null;
  preferredLanguage?: string | null;
}
