using System;
using Apache.NMS;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Decorates a send endpoint so replies can reuse a native Apache NMS reply destination.</summary>
public class ReplyToSendEndpoint :
    SendEndpointProxy
{
    readonly IDestination _destination;

    /// <summary>Creates a reply-aware send-endpoint proxy.</summary>
    /// <param name="endpoint">The underlying send endpoint.</param>
    /// <param name="destination">The native reply destination from the consumed message.</param>
    public ReplyToSendEndpoint(ISendEndpoint endpoint, IDestination destination)
        : base(endpoint)
    {
        _destination = destination;
    }

    /// <summary>Creates a pipeline adapter that applies the native reply destination when addresses match.</summary>
    /// <typeparam name="T">The message type.</typeparam>
    /// <param name="pipe">An optional caller-supplied send pipeline.</param>
    /// <returns>The reply-aware send pipeline.</returns>
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new ReplyToPipe<T>(_destination, pipe);
    }


    class ReplyToPipe<TMessage> :
        SendContextPipeAdapter<TMessage>
        where TMessage : class
    {
        readonly IDestination _destination;

        public ReplyToPipe(IDestination destination, IPipe<SendContext<TMessage>>? pipe)
            : base(pipe)
        {
            _destination = destination;
        }

        protected override void Send(SendContext<TMessage> context)
        {
            if (!context.TryGetPayload(out ConsumeContext? consumeContext))
                return;

            if (!context.TryGetPayload(out ActiveMqSendContext? sendContext))
                throw new ArgumentException("The ActiveMqSendContext was not available");

            if (string.Equals(context.DestinationAddress?.AbsolutePath, consumeContext.ResponseAddress?.AbsolutePath))
                sendContext.ReplyDestination = _destination;
        }

        protected override void Send<T>(SendContext<T> context)
        {
        }
    }
}
