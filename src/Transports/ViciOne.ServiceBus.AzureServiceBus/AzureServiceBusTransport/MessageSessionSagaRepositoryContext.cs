using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.AzureServiceBus;

/// <summary>
/// Provides a message session saga repository context implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class MessageSessionSagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaRepositoryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext;
    readonly ISagaConsumeContextFactory<MessageSessionContext, TSaga> _factory;
    readonly MessageSessionContext _sessionContext;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="factory">The factory value.</param>
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

    /// <summary>
    /// Creates saga consume context.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="instance">The instance value.</param>
    /// <param name="mode">The mode value.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class
    {
        return _factory.CreateSagaConsumeContextAsync(_sessionContext, consumeContext, instance, mode);
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_sessionContext, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

    /// <summary>
    /// Performs the insert operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.Advanced.SagaConsumeContext<TSaga, TMessage>?>(cancellationToken); return Task.FromResult<SagaConsumeContext<TSaga, TMessage>?>(default);
    }

    /// <summary>
    /// Performs the load operation.
    /// </summary>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instance = await ReadSagaStateAsync(_sessionContext).ConfigureAwait(false);
        if (instance == null)
            return default;

        return await _factory.CreateSagaConsumeContextAsync(_sessionContext, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the save operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return WriteSagaStateAsync(_sessionContext, context.Saga);
    }

    /// <summary>
    /// Performs the update operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return WriteSagaStateAsync(_sessionContext, context.Saga);
    }

    /// <summary>
    /// Performs the delete operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        await _sessionContext.SetStateAsync(null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the discard operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    /// <summary>
    /// Performs the undo operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task UndoAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

    static Task WriteSagaStateAsync(MessageSessionContext context, TSaga saga)
    {
        return context.SetStateAsync(BinaryData.FromObjectAsJson(saga, ServiceBusMetadataJson.Options));
    }

    static async Task<TSaga?> ReadSagaStateAsync(MessageSessionContext context)
    {
        var state = await context.GetStateAsync().ConfigureAwait(false);

        return state?.ToObjectFromJson<TSaga>(ServiceBusMetadataJson.Options);
    }
}
