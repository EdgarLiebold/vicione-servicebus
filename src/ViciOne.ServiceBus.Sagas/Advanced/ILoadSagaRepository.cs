using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Loads saga state by correlation identifier.</summary>
/// <typeparam name="TSaga">The persisted saga state type.</typeparam>
public interface ILoadSagaRepository<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>Loads a saga instance when one exists for the correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token that cancels the load.</param>
    /// <returns>A task that produces the saga instance, or <see langword="null" /> when no instance exists.</returns>
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
