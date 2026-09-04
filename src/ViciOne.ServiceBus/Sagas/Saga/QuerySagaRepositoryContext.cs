using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

public interface SagaRepositoryContext<TSaga, TMessage> :
    ISagaConsumeContextFactory<TSaga>,
    ConsumeContext<TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>
    /// Add the saga instance, using the specified <see cref="SagaConsumeContext{TSaga,T}" />
    /// </summary>
    /// <param name="instance"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Insert the saga instance, if it does not already exist.
    /// </summary>
    /// <param name="instance"></param>
    /// <returns>
    /// A valid <see cref="SagaConsumeContext{TSaga,T}" /> if the instance inserted successfully, otherwise default
    /// </returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load an existing saga instance
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns>
    /// A valid <see cref="SagaConsumeContext{TSaga,T}" /> if the instance loaded successfully, otherwise default
    /// </returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save the saga, called after an Add, without an insert
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update the saga, called after a load or insert where the saga has not completed
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete the saga, called after a Load when the saga is completed
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Discard the saga, called after an Add when the saga is completed within the same transaction
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>
    /// Undo the changes for the saga
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);
}


public interface QuerySagaRepositoryContext<TSaga> :
    PipeContext
    where TSaga : class, ISaga
{
    /// <summary>
    /// Query saga instances
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<SagaRepositoryQueryContext<TSaga>> QueryAsync(ISagaQuery<TSaga> query, CancellationToken cancellationToken = default);
}


public interface LoadSagaRepositoryContext<TSaga> :
    PipeContext
    where TSaga : class, ISaga
{
    /// <summary>
    /// Load an existing saga instance
    /// </summary>
    /// <param name="correlationId"></param>
    /// <returns>The saga, if found, or null</returns>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task<TSaga?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);
}
