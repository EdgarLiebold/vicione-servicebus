using System;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Transfers consume headers and optional request lifetime to an outgoing send context.</summary>
/// <typeparam name="TMessage">The outgoing message contract.</typeparam>
internal sealed class ConsumeSendPipeAdapter<TMessage> :
    SendContextPipeAdapter<TMessage>
    where TMessage : class
{
    readonly ConsumeContext _consumeContext;
    readonly bool _inheritRequestTimeToLive;
    readonly Guid? _requestId;

    /// <summary>Initializes a consume-aware pipe without inheriting the request deadline.</summary>
    /// <param name="consumeContext">The consume context that supplies outgoing headers.</param>
    /// <param name="pipe">The caller-supplied typed send pipe.</param>
    /// <param name="requestId">The optional request identifier assigned to the outgoing message.</param>
    internal ConsumeSendPipeAdapter(ConsumeContext consumeContext, IPipe<SendContext<TMessage>> pipe, Guid? requestId)
        : this(consumeContext, pipe, requestId, false)
    {
    }

    internal ConsumeSendPipeAdapter(ConsumeContext consumeContext, IPipe<SendContext<TMessage>>? pipe, Guid? requestId,
        bool inheritRequestTimeToLive)
        : base(pipe)
    {
        _consumeContext = consumeContext ?? throw new ArgumentNullException(nameof(consumeContext));
        _requestId = requestId;
        _inheritRequestTimeToLive = inheritRequestTimeToLive;
    }

    /// <summary>Transfers consume headers, request identity, and an opted-in request deadline.</summary>
    /// <typeparam name="T">The outgoing message contract.</typeparam>
    /// <param name="context">The send context receiving inherited metadata.</param>
    protected override void Send<T>(SendContext<T> context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_requestId.HasValue)
            context.RequestId = _requestId;

        context.TransferConsumeContextHeaders(_consumeContext);

        // A request deadline belongs to the request outcome, not to arbitrary work started by
        // the consumer. Response and fault sends opt in explicitly.
        if (_inheritRequestTimeToLive && _requestId.HasValue && _consumeContext.ExpirationTime.HasValue)
        {
            context.TimeToLive = _consumeContext.ExpirationTime.Value
                - _consumeContext.GetTimeProvider().GetUtcNow().UtcDateTime;

            // The one-second floor keeps an already-late response or fault valid for transport
            // dispatch while bounding how long the outcome may remain deliverable.
            if (context.TimeToLive.Value <= TimeSpan.Zero)
                context.TimeToLive = TimeSpan.FromSeconds(1);
        }
    }

    /// <summary>Leaves typed metadata application to the general send-context path.</summary>
    /// <param name="context">The typed send context already configured by the general path.</param>
    protected override void Send(SendContext<TMessage> context)
    {
        ArgumentNullException.ThrowIfNull(context);
    }
}
