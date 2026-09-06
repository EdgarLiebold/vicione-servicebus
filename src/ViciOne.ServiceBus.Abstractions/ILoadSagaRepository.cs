using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Stores and retrieves load saga data.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface ILoadSagaRepository<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>Loads the requested state.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outcome.</returns>
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
