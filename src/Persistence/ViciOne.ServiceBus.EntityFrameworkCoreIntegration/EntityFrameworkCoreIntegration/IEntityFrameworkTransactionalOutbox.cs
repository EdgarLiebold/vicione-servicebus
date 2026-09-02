namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;


/// <summary>
/// Explicit transactional messaging session bound to one bus and the caller's DbContext. Commit persists business
/// changes and staged outbox records through that same DbContext; Abort detaches only this session's staged outbox records.
/// </summary>
public interface IEntityFrameworkTransactionalOutbox<TBus, TDbContext>
    where TBus : class, IBus
    where TDbContext : DbContext
{
    ISendEndpointProvider SendEndpointProvider { get; }
    IPublishEndpoint PublishEndpoint { get; }
    IScopedClientFactory ClientFactory { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);
    Task AbortAsync(CancellationToken cancellationToken = default);
}
