using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Provides correlation-identifier lookup within an open saga repository operation.</summary>
/// <typeparam name="TSaga">The persisted saga state type.</typeparam>
public interface ILoadSagaRepositoryContext<TSaga> : PipeContext
    where TSaga : class, ISaga
{
    /// <summary>Loads a saga instance by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The saga when found; otherwise, <see langword="null" />.</returns>
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
