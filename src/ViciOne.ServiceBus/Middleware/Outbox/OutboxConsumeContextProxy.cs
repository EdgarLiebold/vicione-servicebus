using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>Forwards outbox consume context operations to an underlying context.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public abstract class OutboxConsumeContextProxy<TMessage> :
    ConsumeContextProxy<TMessage>,
    OutboxConsumeContext<TMessage>
    where TMessage : class
{
    readonly IServiceProvider _provider;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="provider">The service provider used to resolve dependencies.</param>
    protected OutboxConsumeContextProxy(ConsumeContext<TMessage> context, OutboxConsumeOptions options, IServiceProvider provider)
        : base(context)
    {
        CapturedContext = context.Advanced();
        Options = options;
        _provider = provider;

        var outboxReceiveContext = new OutboxReceiveContext(this, context.Advanced().ReceiveContext);

        ReceiveContext = outboxReceiveContext;
        PublishEndpointProvider = outboxReceiveContext.PublishEndpointProvider;

        if (context.TryGetPayload(out MessageSchedulerContext? schedulerContext))
        {
            context.AddOrUpdatePayload<MessageSchedulerContext>(
                () => new ConsumeMessageSchedulerContext(this, schedulerContext.SchedulerFactory),
                existing => new ConsumeMessageSchedulerContext(this, existing.SchedulerFactory));
        }
    }

    /// <summary>Gets the options.</summary>
    protected OutboxConsumeOptions Options { get; }

    /// <summary>Gets the consumer id.</summary>
    protected Guid ConsumerId => Options.ConsumerId;

    /// <summary>Gets the captured context.</summary>
    public ConsumeContext CapturedContext { get; }

    /// <summary>Gets or sets the continue processing.</summary>
    public abstract bool ContinueProcessing { get; set; }
    /// <summary>Gets a value indicating whether message consumed.</summary>
    public abstract bool IsMessageConsumed { get; }
    /// <summary>Gets a value indicating whether outbox delivered.</summary>
    public abstract bool IsOutboxDelivered { get; }
    /// <summary>Gets the receive count.</summary>
    public abstract int ReceiveCount { get; }
    /// <summary>Gets the last sequence number.</summary>
    public abstract long? LastSequenceNumber { get; }

    /// <summary>Sets consumed.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task SetConsumedAsync(CancellationToken cancellationToken = default);
    /// <summary>Sets delivered.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task SetDeliveredAsync(CancellationToken cancellationToken = default);

    /// <summary>Loads outbox messages.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that produces the load outbox messages outcome.</returns>
    public abstract Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Reports that notify outbox message has been delivered.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default);

    /// <summary>Removes outbox messages.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds send to the configuration.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="context">The context associated with the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public abstract Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Gets service.</summary>
    /// <param name="serviceType">The runtime service type used by the operation.</param>
    /// <returns>The service.</returns>
    public object? GetService(Type serviceType)
    {
        return _provider.GetService(serviceType);
    }
}
