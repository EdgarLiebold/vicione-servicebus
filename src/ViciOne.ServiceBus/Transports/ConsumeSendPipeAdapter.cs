using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Provides a consume send pipe adapter implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class ConsumeSendPipeAdapter<TMessage> :
    SendContextPipeAdapter<TMessage>
    where TMessage : class
{
    readonly ConsumeContext _consumeContext;
    readonly bool _inheritRequestTimeToLive;
    readonly Guid? _requestId;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="consumeContext">The consume context value.</param>
    /// <param name="pipe">The pipe value.</param>
    /// <param name="requestId">The request id value.</param>
    public ConsumeSendPipeAdapter(ConsumeContext consumeContext, IPipe<SendContext<TMessage>> pipe, Guid? requestId)
        : this(consumeContext, pipe, requestId, false)
    {
    }

    internal ConsumeSendPipeAdapter(ConsumeContext consumeContext, IPipe<SendContext<TMessage>>? pipe, Guid? requestId,
        bool inheritRequestTimeToLive)
        : base(pipe)
    {
        _consumeContext = consumeContext;
        _requestId = requestId;
        _inheritRequestTimeToLive = inheritRequestTimeToLive;
    }

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="context">The operation context.</param>
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

    /// <summary>
    /// Sends a message to the configured destination.
    /// </summary>
    /// <param name="context">The operation context.</param>
    protected override void Send(SendContext<TMessage> context)
    {
    }
}
