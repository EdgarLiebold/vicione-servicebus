using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;
/// <summary>
/// Explicit transactional messaging session bound to one bus and the caller's DbContext. Commit persists business
/// changes and staged outbox records through that same DbContext; Abort detaches only this session's staged outbox records.
/// </summary>
public interface IEntityFrameworkTransactionalOutbox<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    /// <summary>
    /// Gets the send endpoint provider value.
    /// </summary>
    ISendEndpointProvider SendEndpointProvider { get; }
    /// <summary>
    /// Gets the publish endpoint value.
    /// </summary>
    IPublishEndpoint PublishEndpoint { get; }
    /// <summary>
    /// Gets the client factory value.
    /// </summary>
    IScopedClientFactory ClientFactory { get; }

    /// <summary>
    /// Performs the commit operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task CommitAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Performs the abort operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task AbortAsync(CancellationToken cancellationToken = default);
}
