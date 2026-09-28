using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Explicit transactional messaging session bound to one bus and the caller's DbContext. Commit persists business
/// changes and staged outbox records through that same DbContext; Abort detaches only this session's staged outbox records.
/// A caller-owned database transaction remains under the caller's control. Rolling it back also rolls back the outbox writes.
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
/// <typeparam name="TDbContext">The DbContext type that owns business and outbox state.</typeparam>
public interface IEntityFrameworkTransactionalOutbox<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    /// <summary>Gets a send endpoint provider that stages messages in the bound DbContext.</summary>
    ISendEndpointProvider SendEndpointProvider { get; }
    /// <summary>Gets a publish endpoint that stages messages in the bound DbContext.</summary>
    IPublishEndpoint PublishEndpoint { get; }
    /// <summary>Gets request clients whose outgoing messages use this transactional session.</summary>
    IScopedClientFactory ClientFactory { get; }

    /// <summary>Saves business changes and staged outgoing messages through the bound DbContext.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when business changes and staged messages have been saved in the bound DbContext.
    /// If the caller owns an outer database transaction, its commit or rollback still determines durability.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
    /// <summary>Detaches this session's uncommitted outbox rows from the bound DbContext.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when this session's staged rows have been detached.</returns>
    Task AbortAsync(CancellationToken cancellationToken = default);
}
