namespace Stratix.Application.DTOs.Branding;

public record OrganizationBrandingResponse(
    long OrganizationId,
    string OrganizationName,
    string? LogoUrl,
    bool CanUploadLogo);

public record OrganizationLogoUploadResult(
    string LogoUrl);
