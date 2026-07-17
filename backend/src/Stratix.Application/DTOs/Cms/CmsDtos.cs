namespace Stratix.Application.DTOs.Cms;

public record CompanyAdminModulePermissionResponse(
    long Id,
    string ModuleCode,
    bool VisibleToCompanyAdmin,
    bool WritableByCompanyAdmin,
    int SortOrder,
    DateTimeOffset UpdatedAt);

public record UpdateCompanyAdminModulePermissionItem(
    string ModuleCode,
    bool VisibleToCompanyAdmin,
    bool WritableByCompanyAdmin);

public record UpdateCompanyAdminPermissionsRequest(
    IReadOnlyList<UpdateCompanyAdminModulePermissionItem> Modules);
