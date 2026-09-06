using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;
/// <summary>
/// Explicit transactional messaging session bound to one bus and the caller's DbContext. Commit persists business
/// changes and staged outbox records through that same DbContext; Abort detaches only this session's staged outbox records.
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
/// <typeparam name="TDbContext">The db context type.</typeparam>
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
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
    /// <summary>Detaches this session's uncommitted outbox rows from the bound DbContext.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task AbortAsync(CancellationToken cancellationToken = default);
}
