using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Outbox;

/// <summary>
/// Provides an outbox consume context proxy implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public abstract class OutboxConsumeContextProxy<TMessage> :
    ConsumeContextProxy<TMessage>,
    OutboxConsumeContext<TMessage>
    where TMessage : class
{
    readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <param name="options">The options value.</param>
    /// <param name="provider">The service provider.</param>
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

    /// <summary>
    /// Gets the options value.
    /// </summary>
    protected OutboxConsumeOptions Options { get; }

    /// <summary>
    /// Gets the consumer id value.
    /// </summary>
    protected Guid ConsumerId => Options.ConsumerId;

    /// <summary>
    /// Gets the captured context value.
    /// </summary>
    public ConsumeContext CapturedContext { get; }

    /// <summary>
    /// Gets or sets the continue processing value.
    /// </summary>
    public abstract bool ContinueProcessing { get; set; }
    /// <summary>
    /// Gets the is message consumed value.
    /// </summary>
    public abstract bool IsMessageConsumed { get; }
    /// <summary>
    /// Gets the is outbox delivered value.
    /// </summary>
    public abstract bool IsOutboxDelivered { get; }
    /// <summary>
    /// Gets the receive count value.
    /// </summary>
    public abstract int ReceiveCount { get; }
    /// <summary>
    /// Gets the last sequence number value.
    /// </summary>
    public abstract long? LastSequenceNumber { get; }

    /// <summary>
    /// Sets consumed.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task SetConsumedAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Sets delivered.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task SetDeliveredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the load outbox messages operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the notify outbox message delivered operation.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the remove outbox messages operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds send to the configuration.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public abstract Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <param name="serviceType">The service type value.</param>
    /// <returns>The result of the operation.</returns>
    public object? GetService(Type serviceType)
    {
        return _provider.GetService(serviceType);
    }
}
