using System;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for load saga repository.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ILoadSagaRepository<TSaga> :
    IProbeSite
    where TSaga : class, ISaga
{
    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
