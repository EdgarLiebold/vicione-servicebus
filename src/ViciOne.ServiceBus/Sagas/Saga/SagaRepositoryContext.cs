using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Provides persistence operations for a saga handling one message.</summary>
public interface SagaRepositoryContext<TSaga, TMessage> :
    ISagaConsumeContextFactory<TSaga>,
    ConsumeContext<TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    /// <summary>Adds a new saga instance.</summary>
    /// <param name="instance">The saga instance to add.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The consume context that owns the added saga.</returns>
    Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default);

    /// <summary>Inserts a saga instance when it does not already exist.</summary>
    /// <param name="instance">The saga instance to insert.</param>
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
    /// <param name="context">The completed saga consume context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the delete operation.</returns>
    Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Discards a saga that completed in the transaction that added it.</summary>
    /// <param name="context">The completed saga consume context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the discard operation.</returns>
    Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);

    /// <summary>Reverts uncommitted changes to the saga.</summary>
    /// <param name="context">The saga consume context whose changes are reverted.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the undo operation.</returns>
    Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default);
}
