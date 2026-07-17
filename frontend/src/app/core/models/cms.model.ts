export interface CompanyAdminModulePermission {
  id: number;
  moduleCode: string;
  visibleToCompanyAdmin: boolean;
  writableByCompanyAdmin: boolean;
  sortOrder: number;
  updatedAt: string;
}

export interface UpdateCompanyAdminModulePermissionItem {
  moduleCode: string;
  visibleToCompanyAdmin: boolean;
  writableByCompanyAdmin: boolean;
}

export interface UpdateCompanyAdminPermissionsRequest {
  modules: UpdateCompanyAdminModulePermissionItem[];
}
