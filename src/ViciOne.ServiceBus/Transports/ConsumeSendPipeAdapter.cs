using System;

namespace ViciOne.ServiceBus.Transports;

public class ConsumeSendPipeAdapter<TMessage> :
    SendContextPipeAdapter<TMessage>
    where TMessage : class
{
    readonly ConsumeContext _consumeContext;
    readonly bool _inheritRequestTimeToLive;
    readonly Guid? _requestId;

    public ConsumeSendPipeAdapter(ConsumeContext consumeContext, IPipe<SendContext<TMessage>> pipe, Guid? requestId)
        : this(consumeContext, pipe, requestId, false)
    {
    }

    internal ConsumeSendPipeAdapter(ConsumeContext consumeContext, IPipe<SendContext<TMessage>> pipe, Guid? requestId,
        bool inheritRequestTimeToLive)
        : base(pipe)
    {
        _consumeContext = consumeContext;
        _requestId = requestId;
        _inheritRequestTimeToLive = inheritRequestTimeToLive;
    }

    protected override void Send<T>(SendContext<T> context)
    {
        if (_requestId.HasValue)
            context.RequestId = _requestId;

        if (_consumeContext != null)
        {
            context.TransferConsumeContextHeaders(_consumeContext);

            // A request deadline belongs to the request outcome, not to arbitrary work started by
            // the consumer. Response and fault endpoint resolution opt in explicitly so custom fault
            // addresses and publish fallbacks do not have to be inferred from the destination URI.
            if (_inheritRequestTimeToLive && _requestId.HasValue && _consumeContext.ExpirationTime.HasValue)
            {
                context.TimeToLive = _consumeContext.ExpirationTime.Value
                    - _consumeContext.GetTimeProvider().GetUtcNow().UtcDateTime;

                // The deadline may pass while the consumer is producing its response or fault. The
                // one-second floor keeps the transport TTL valid while bounding the lifetime of that
                // already-late outcome. It must never be applied to unrelated sends or publishes.
                if (context.TimeToLive.Value <= TimeSpan.Zero)
                    context.TimeToLive = TimeSpan.FromSeconds(1);
            }
        }
    }

    protected override void Send(SendContext<TMessage> context)
    {
    }
}
