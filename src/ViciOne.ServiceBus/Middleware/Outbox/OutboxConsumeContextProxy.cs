using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.Middleware.Outbox;

public abstract class OutboxConsumeContextProxy<TMessage> :
    ConsumeContextProxy<TMessage>,
    OutboxConsumeContext<TMessage>
    where TMessage : class
{
    readonly IServiceProvider _provider;

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

    protected OutboxConsumeOptions Options { get; }

    protected Guid ConsumerId => Options.ConsumerId;

    public ConsumeContext CapturedContext { get; }

    public abstract bool ContinueProcessing { get; set; }
    public abstract bool IsMessageConsumed { get; }
    public abstract bool IsOutboxDelivered { get; }
    public abstract int ReceiveCount { get; }
    public abstract long? LastSequenceNumber { get; }

    public abstract Task SetConsumedAsync(CancellationToken cancellationToken = default);
    public abstract Task SetDeliveredAsync(CancellationToken cancellationToken = default);

    public abstract Task<List<OutboxMessageContext>> LoadOutboxMessagesAsync(CancellationToken cancellationToken = default);

    public abstract Task NotifyOutboxMessageDeliveredAsync(OutboxMessageContext message, CancellationToken cancellationToken = default);

    public abstract Task RemoveOutboxMessagesAsync(CancellationToken cancellationToken = default);

    public abstract Task AddSendAsync<T>(SendContext<T> context, CancellationToken cancellationToken = default)
        where T : class;

    public object? GetService(Type serviceType)
    {
        return _provider.GetService(serviceType);
    }
}
