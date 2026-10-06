using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Provides persistence operations for a saga handling one message.</summary>
/// <typeparam name="TSaga">The persisted saga state type.</typeparam>
/// <typeparam name="TMessage">The consumed message type.</typeparam>
public interface ISagaRepositoryContext<TSaga, TMessage> :
    ISagaConsumeContextFactory<TSaga>,
    ConsumeContext<TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Adds a new saga instance.</summary>
    /// <param name="instance">The new saga state.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The consume context that owns the added saga.</returns>
    Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default);

    /// <summary>Inserts a saga instance when it does not already exist.</summary>
    /// <param name="instance">The new saga state.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The inserted saga context, or <see langword="null" /> when the saga already exists.</returns>
    Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default);

    /// <summary>Loads a saga instance by correlation identifier.</summary>
    /// <param name="correlationId">The saga correlation identifier.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The loaded saga context, or <see langword="null" /> when the saga is absent.</returns>
    Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default);

    /// <summary>Persists a saga added without an insert.</summary>
    /// <param name="context">The saga consume context to persist.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the persistence operation.</returns>
    Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Persists changes to a saga that has not completed.</summary>
    /// <param name="context">The saga consume context to update.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the persistence operation.</returns>
    Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Deletes a completed saga loaded from persistence.</summary>
    /// <param name="context">The loaded saga context to delete.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the delete operation.</returns>
    Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Discards a saga that completed in the transaction that added it.</summary>
    /// <param name="context">The newly added saga context to discard.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the discard operation.</returns>
    Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Performs provider-specific undo handling; the in-memory repository leaves referenced saga state unchanged.</summary>
    /// <param name="context">The saga consume context supplied to the repository provider's undo policy.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the undo operation.</returns>
    Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);
}
