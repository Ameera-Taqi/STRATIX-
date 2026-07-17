namespace Stratix.Application.DTOs.Organizations;

public record OrganizationResponse(
    long Id, string Name, string Slug, string Status, string SubscriptionPlan,
    DateTimeOffset CreatedAt, int UserCount, int ProjectCount);

public record PlanResponse(
    long Id, string Name, int MaxUsers, int MaxProjects, bool AiEnabled, long StorageLimitMb, decimal Price);

public record SubscriptionResponse(
    long OrganizationId, string PlanCode, string Status,
    DateTimeOffset StartedAt, DateTimeOffset? TrialEndsAt, DateTimeOffset? EndsAt);
