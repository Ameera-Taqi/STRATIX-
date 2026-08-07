using Microsoft.EntityFrameworkCore;
using Stratix.Application.Common;
using Stratix.Application.DTOs.Onboarding;
using Stratix.Application.Interfaces;
using Stratix.Domain.Enums;

namespace Stratix.Application.Services;

public interface IOrganizationOnboardingService
{
    Task<OrganizationOnboardingStatusResponse> GetStatusAsync(CancellationToken ct = default);
    Task<OrganizationOnboardingStatusResponse> UpdateProfileAsync(UpdateOrganizationProfileRequest request, CancellationToken ct = default);
    Task<OrganizationOnboardingStatusResponse> CompleteAsync(CancellationToken ct = default);
}

public class OrganizationOnboardingService : IOrganizationOnboardingService
{
    private static readonly HashSet<string> AllowedLanguages = new(StringComparer.OrdinalIgnoreCase) { "en", "ar" };

    private readonly IApplicationDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IOrganizationBrandingService _branding;

    public OrganizationOnboardingService(
        IApplicationDbContext db,
        ITenantContext tenant,
        IOrganizationBrandingService branding)
    {
        _db = db;
        _tenant = tenant;
        _branding = branding;
    }

    public async Task<OrganizationOnboardingStatusResponse> GetStatusAsync(CancellationToken ct = default)
    {
        var org = await RequireOrgAsync(ct);
        return await ToStatusAsync(org, ct);
    }

    public async Task<OrganizationOnboardingStatusResponse> UpdateProfileAsync(
        UpdateOrganizationProfileRequest request,
        CancellationToken ct = default)
    {
        var org = await RequireOrgAsync(ct);

        if (!string.IsNullOrWhiteSpace(request.OrganizationName))
            org.Name = request.OrganizationName.Trim();

        if (request.Industry is not null)
            org.Industry = string.IsNullOrWhiteSpace(request.Industry) ? null : request.Industry.Trim();

        if (request.Timezone is not null)
            org.Timezone = string.IsNullOrWhiteSpace(request.Timezone) ? null : request.Timezone.Trim();

        if (request.PreferredLanguage is not null)
        {
            var lang = request.PreferredLanguage.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(lang) && !AllowedLanguages.Contains(lang))
                throw new ArgumentException("Preferred language must be 'en' or 'ar'.");
            org.PreferredLanguage = string.IsNullOrEmpty(lang) ? null : lang;
        }

        org.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await ToStatusAsync(org, ct);
    }

    public async Task<OrganizationOnboardingStatusResponse> CompleteAsync(CancellationToken ct = default)
    {
        var org = await RequireOrgAsync(ct);
        if (org.OnboardingCompletedAt is null)
        {
            org.OnboardingCompletedAt = DateTimeOffset.UtcNow;
            org.UpdatedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return await ToStatusAsync(org, ct);
    }

    private async Task<OrganizationOnboardingStatusResponse> ToStatusAsync(
        Domain.Entities.Organization org,
        CancellationToken ct)
    {
        var branding = await _branding.GetAsync(ct);
        var hasDepartment = await _db.Departments.AnyAsync(ct);
        var hasTeamMember = await _db.Users.CountAsync(ct) > 1;
        var hasProject = await _db.Projects.AnyAsync(ct);

        return new OrganizationOnboardingStatusResponse(
            org.Id,
            org.Name,
            branding.LogoUrl,
            branding.CanUploadLogo,
            org.Industry,
            org.Timezone,
            org.PreferredLanguage,
            org.OnboardingCompletedAt is not null,
            hasDepartment,
            hasTeamMember,
            hasProject);
    }

    private async Task<Domain.Entities.Organization> RequireOrgAsync(CancellationToken ct)
    {
        if (_tenant.OrganizationId is not long orgId)
            throw new UnauthorizedAccessException("No organization context.");
        return await _db.Organizations.FirstOrDefaultAsync(o => o.Id == orgId, ct)
            ?? throw new KeyNotFoundException("Organization not found.");
    }
}
