namespace Stratix.Api.Auth;

/// <summary>
/// Named authorization policies that replace repeated role-string lists on controllers.
/// </summary>
public static class AuthPolicies
{
    /// <summary>Platform operator only.</summary>
    public const string SuperAdmin = "SuperAdmin";

    /// <summary>Tenant / org administration (users, departments, roles, branding).</summary>
    public const string OrgAdmins = "OrgAdmins";

    /// <summary>Can create and manage projects, features, risks, files.</summary>
    public const string ProjectManagers = "ProjectManagers";

    /// <summary>Can assign / lead work (includes team leaders).</summary>
    public const string TeamLeaders = "TeamLeaders";

    /// <summary>Can update task status (includes employees on assigned tasks).</summary>
    public const string TaskContributors = "TaskContributors";

    /// <summary>Read dashboards, KPI, audit (excludes plain employees).</summary>
    public const string LeadersAndExecutives = "LeadersAndExecutives";

    /// <summary>AI health / analysis consumers.</summary>
    public const string AiAnalysts = "AiAnalysts";

    /// <summary>Any authenticated tenant role (full product read surface).</summary>
    public const string AllTenantUsers = "AllTenantUsers";
}
