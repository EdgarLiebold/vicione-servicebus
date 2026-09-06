using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Loads saga instances from a repository.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public interface LoadSagaRepositoryContext<TSaga> : PipeContext
    where TSaga : class, ISaga
{
    /// <summary>Loads a saga instance by correlation identifier.</summary>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The saga when found; otherwise, <see langword="null" />.</returns>
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
