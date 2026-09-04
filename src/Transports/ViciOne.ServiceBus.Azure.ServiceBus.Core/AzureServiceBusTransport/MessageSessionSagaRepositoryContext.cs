using System;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Serialization;

namespace ViciOne.ServiceBus.AzureServiceBusTransport;

public class MessageSessionSagaRepositoryContext<TSaga, TMessage> :
    ConsumeContextScope<TMessage>,
    SagaRepositoryContext<TSaga, TMessage>
    where TSaga : class, ISaga
    where TMessage : class
{
    readonly ConsumeContext<TMessage> _consumeContext;
    readonly ISagaConsumeContextFactory<MessageSessionContext, TSaga> _factory;
    readonly MessageSessionContext _sessionContext;

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

    public Task<SagaConsumeContext<TSaga, T>> CreateSagaConsumeContextAsync<T>(ConsumeContext<T> consumeContext, TSaga instance,
        SagaConsumeContextMode mode)
        where T : class
    {
        return _factory.CreateSagaConsumeContextAsync(_sessionContext, consumeContext, instance, mode);
    }

    public Task<SagaConsumeContext<TSaga, TMessage>> AddAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.SagaConsumeContext<TSaga, TMessage>>(cancellationToken); return _factory.CreateSagaConsumeContextAsync(_sessionContext, _consumeContext, instance, SagaConsumeContextMode.Add);
    }

    public Task<SagaConsumeContext<TSaga, TMessage>?> InsertAsync(TSaga instance, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.SagaConsumeContext<TSaga, TMessage>?>(cancellationToken); return Task.FromResult<SagaConsumeContext<TSaga, TMessage>?>(default);
    }

    public async Task<SagaConsumeContext<TSaga, TMessage>?> LoadAsync(Guid correlationId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested(); var instance = await ReadSagaStateAsync(_sessionContext).ConfigureAwait(false);
        if (instance == null)
            return default;

        return await _factory.CreateSagaConsumeContextAsync(_sessionContext, _consumeContext, instance, SagaConsumeContextMode.Load).ConfigureAwait(false);
    }

    public Task SaveAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return WriteSagaStateAsync(_sessionContext, context.Saga);
    }

    public Task UpdateAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return WriteSagaStateAsync(_sessionContext, context.Saga);
    }

    public async Task DeleteAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        await _sessionContext.SetStateAsync(null, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    public Task DiscardAsync(SagaConsumeContext<TSaga> context, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask;
    }

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
