namespace Stratix.Domain.Entities;

/// <summary>
/// Soft-delete contract. Persistence converts physical deletes into
/// <see cref="IsDeleted"/> updates and hides soft-deleted rows via query filters.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}
