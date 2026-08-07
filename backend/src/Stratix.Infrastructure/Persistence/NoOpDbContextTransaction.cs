using Microsoft.EntityFrameworkCore.Storage;

namespace Stratix.Infrastructure.Persistence;

/// <summary>
/// No-op transaction for non-relational providers (e.g. EF InMemory in tests).
/// </summary>
internal sealed class NoOpDbContextTransaction : IDbContextTransaction
{
    public Guid TransactionId { get; } = Guid.NewGuid();

    public void Commit() { }
    public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Rollback() { }
    public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    public void Dispose() { }
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
