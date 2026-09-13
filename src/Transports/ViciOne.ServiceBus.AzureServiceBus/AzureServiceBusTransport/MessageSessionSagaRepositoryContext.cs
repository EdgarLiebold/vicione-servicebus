using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>Reads and writes saga state through the active Azure Service Bus session.</summary>
/// <typeparam name="TSaga">The session-backed saga state type.</typeparam>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public class MessageSessionSagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    ISagaRepositoryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext;
    readonly ISagaConsumeContextFactory<MessageSessionContext, TSaga> _factory;
    readonly MessageSessionContext _sessionContext;

    /// <summary>Creates a repository context from a consume context that contains active session metadata.</summary>
    /// <param name="consumeContext">The typed consume context.</param>
    /// <param name="factory">The factory that creates saga consume contexts.</param>
    public MessageSessionSagaRepositoryContext(ConsumeContext<TMessage> consumeContext, ISagaConsumeContextFactory<MessageSessionContext, TSaga> factory)
        : base(consumeContext)
    {
        if (!consumeContext.TryGetPayload(out MessageSessionContext? sessionContext))
        {
            throw new SagaException($"The session-based saga repository requires an active message session: {TypeCache<TSaga>.ShortName}",
                typeof(TSaga), typeof(TMessage));
        }

        _consumeContext = consumeContext;
        _sessionContext = sessionContext;
        _factory = factory;
    }

    /// <summary>Creates a saga consume context associated with the active session.</summary>
    /// <typeparam name="T">The consumed message contract.</typeparam>
    /// <param name="consumeContext">The typed consume context.</param>
    /// <param name="instance">The saga state instance.</param>
    /// <param name="mode">Whether the state is being added or loaded.</param>
    /// <returns>A task that produces the saga consume context.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class
    {
        return _factory.CreateSagaConsumeContextAsync(_sessionContext, consumeContext, instance, mode);
    }

    /// <summary>Creates an add-mode saga consume context for a new session state.</summary>
    /// <param name="instance">The new saga state.</param>
    /// <param name="cancellationToken">Cancels before context creation begins.</param>
    /// <returns>A task that produces the saga consume context.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_sessionContext, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

    /// <summary>Returns no inserted context because session state is created through <see cref="AddAsync"/>.</summary>
    /// <param name="instance">The unused saga state.</param>
    /// <param name="cancellationToken">Returns a canceled task when cancellation is already requested.</param>
    /// <returns>A task whose result is always <see langword="null"/>.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SagaConsumeContext<TSaga, TMessage>?>(cancellationToken); return Task.FromResult<SagaConsumeContext<TSaga, TMessage>?>(default);
    }

    /// <summary>Loads the saga state stored in the active session.</summary>
    /// <param name="correlationId">The requested correlation identifier; the active session selects the state.</param>
    /// <param name="cancellationToken">Cancels the session-state read.</param>
    /// <returns>A task that produces the saga consume context, or <see langword="null"/> when the session has no state.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        TSaga? instance = await ReadSagaStateAsync(_sessionContext, cancellationToken).ConfigureAwait(false);
        if (instance == null)
            return default;

        return await _factory.CreateSagaConsumeContextAsync(_sessionContext, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
    }

    /// <summary>Serializes and stores the saga state in the active session.</summary>
    /// <param name="context">The saga consume context containing the state.</param>
    /// <param name="cancellationToken">Cancels the session-state write.</param>
    /// <returns>A task that completes when Azure Service Bus stores the state.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return WriteSagaStateAsync(_sessionContext, context.Saga, cancellationToken);
    }

    /// <summary>Replaces the saga state stored in the active session.</summary>
    /// <param name="context">The saga consume context containing the updated state.</param>
    /// <param name="cancellationToken">Cancels the session-state write.</param>
    /// <returns>A task that completes when Azure Service Bus stores the state.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        return WriteSagaStateAsync(_sessionContext, context.Saga, cancellationToken);
    }

    /// <summary>Deletes the saga by clearing the active session state.</summary>
    /// <param name="context">The saga consume context being deleted.</param>
    /// <param name="cancellationToken">Cancels the session-state write.</param>
    /// <returns>A task that completes when Azure Service Bus clears the state.</returns>
    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        await _sessionContext.SetStateAsync(null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Completes without changing session state because unsaved add-mode state requires no rollback.</summary>
    /// <param name="context">The discarded saga consume context.</param>
    /// <param name="cancellationToken">Returns a canceled task when cancellation is already requested.</param>
    /// <returns>A completed task when cancellation was not requested.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>Completes without changing session state because writes occur only during save or update.</summary>
    /// <param name="context">The saga consume context whose pending operation is abandoned.</param>
    /// <param name="cancellationToken">Returns a canceled task when cancellation is already requested.</param>
    /// <returns>A completed task when cancellation was not requested.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    internal Task<TSaga?> ReadCurrentAsync(CancellationToken cancellationToken) =>
        ReadSagaStateAsync(_sessionContext, cancellationToken);

    static Task WriteSagaStateAsync(MessageSessionContext context, TSaga saga, CancellationToken cancellationToken)
    {
        return context.SetStateAsync(BinaryData.FromObjectAsJson(saga, ServiceBusMetadataJson.Options), cancellationToken);
    }

    static async Task<TSaga?> ReadSagaStateAsync(MessageSessionContext context, CancellationToken cancellationToken)
    {
        var state = await context.GetStateAsync(cancellationToken).ConfigureAwait(false);

        return state?.ToObjectFromJson<TSaga>(ServiceBusMetadataJson.Options);
    }
}
