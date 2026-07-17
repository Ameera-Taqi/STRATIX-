export interface OrganizationBranding {
  organizationId: number;
  organizationName: string;
  logoUrl: string | null;
  canUploadLogo: boolean;
}

export interface OrganizationLogoUploadResult {
  logoUrl: string;
}
