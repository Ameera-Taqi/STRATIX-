namespace Stratix.Domain.Entities;

/// <summary>Entity that tracks creation time (auto-stamped on insert).</summary>
public interface IHasCreatedAt
{
    DateTimeOffset CreatedAt { get; set; }
}

/// <summary>Entity that tracks last update time (auto-stamped on insert/update).</summary>
public interface IHasUpdatedAt
{
    DateTimeOffset UpdatedAt { get; set; }
}
