using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Decorates a consume context so outgoing messages are captured by a durable outbox.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public abstract class OutboxConsumeContextProxy<TMessage> :
    ConsumeContextProxy<TMessage>,
    OutboxConsumeContext<TMessage>
    where TMessage : class
{
    readonly IServiceProvider _provider;

    /// <summary>Initializes the outbox decorator over an existing consume context.</summary>
    /// <param name="context">The consume context whose outgoing operations are captured.</param>
    /// <param name="options">The delivery and consumer identity settings for the outbox.</param>
    /// <param name="provider">The scoped service provider exposed through the context.</param>
    protected OutboxConsumeContextProxy(ConsumeContext<TMessage> context, OutboxConsumeOptions options, IServiceProvider provider)
        : base(context ?? throw new ArgumentNullException(nameof(context)))
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(provider);

        ConsumeContext capturedContext = context.Advanced();
        CapturedContext = capturedContext;
        Options = options;
        _provider = provider;

        var outboxReceiveContext = new OutboxReceiveContext(this, capturedContext.ReceiveContext);

        ReceiveContext = outboxReceiveContext;
        SetPublishEndpointProvider(outboxReceiveContext.PublishEndpointProvider);

        if (context.TryGetPayload(out MessageSchedulerContext? schedulerContext))
        {
            context.AddOrUpdatePayload<MessageSchedulerContext>(
                () => new ConsumeMessageSchedulerContext(this, schedulerContext.SchedulerFactory),
                existing => new ConsumeMessageSchedulerContext(this, existing.SchedulerFactory));
        }
    }

    /// <summary>Gets the outbox delivery and consumer identity settings.</summary>
    protected OutboxConsumeOptions Options { get; }

    /// <summary>Gets the stable identity of the consumer whose inbox is being processed.</summary>
    protected Guid ConsumerId => Options.ConsumerId;

    /// <summary>Gets the original consume context outside the outbox decorator.</summary>
    public ConsumeContext CapturedContext { get; }

    /// <summary>Gets or sets whether the receive pipeline may continue after outbox processing.</summary>
    public abstract bool ContinueProcessing { get; set; }
    /// <summary>Gets a value indicating whether consumption has been committed to the inbox.</summary>
    public abstract bool IsMessageConsumed { get; }
    /// <summary>Gets a value indicating whether every captured outbox message has been delivered.</summary>
    public abstract bool IsOutboxDelivered { get; }
    /// <summary>Gets the number of attempts made to process the inbox message.</summary>
    public abstract int ReceiveCount { get; }
    /// <summary>Gets the sequence number of the last delivered outbox message, if any.</summary>
    public abstract long? LastSequenceNumber { get; }

    /// <summary>Persists that the incoming message has been consumed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the consumed state has been stored.</returns>
    public abstract Task SetConsumedAsync(CancellationToken cancellationToken = default);
    /// <summary>Persists that every captured outbox message has been delivered.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the delivered state has been stored.</returns>
    public abstract Task SetDeliveredAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads the next ordered set of captured messages awaiting delivery.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task containing the messages awaiting delivery.</returns>
    public abstract Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists the delivery progress for one outbox message.</summary>
    /// <param name="message">The delivered outbox message.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the delivery checkpoint has been stored.</returns>
    public abstract Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default);

    /// <summary>Removes messages whose delivery has been committed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the delivered messages have been removed.</returns>
    public abstract Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Captures an outgoing send for later delivery by the outbox.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The populated send context to capture.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes when the outgoing message has been captured.</returns>
    public abstract Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Resolves an optional service from the consume scope.</summary>
    /// <param name="serviceType">The service type to resolve.</param>
    /// <returns>The resolved service, or <see langword="null"/> when it is not registered.</returns>
    public object? GetService(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return _provider.GetService(serviceType);
    }
}
