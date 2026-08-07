namespace Stratix.Application.DTOs.Onboarding;

public record OrganizationOnboardingStatusResponse(
    long OrganizationId,
    string OrganizationName,
    string? LogoUrl,
    bool CanUploadLogo,
    string? Industry,
    string? Timezone,
    string? PreferredLanguage,
    bool OnboardingCompleted,
    bool HasDepartment,
    bool HasTeamMember,
    bool HasProject);

public record UpdateOrganizationProfileRequest(
    string? OrganizationName,
    string? Industry,
    string? Timezone,
    string? PreferredLanguage);
