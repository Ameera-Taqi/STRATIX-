namespace Stratix.Domain.Entities;

/// <summary>
/// Platform CMS setting: which modules company admins (ORG_ADMIN / ADMIN) can see and write.
/// Not tenant-scoped — one row per module for the whole platform.
/// </summary>
public class PlatformModulePermission
{
    public long Id { get; set; }
    public string ModuleCode { get; set; } = string.Empty;
    public bool VisibleToCompanyAdmin { get; set; } = true;
    public bool WritableByCompanyAdmin { get; set; } = true;
    public int SortOrder { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
