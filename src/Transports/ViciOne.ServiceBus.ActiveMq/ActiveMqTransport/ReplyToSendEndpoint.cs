using System;
using Apache.NMS;
using ViciOne.ServiceBus.Transports;

#nullable enable
namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides a reply to send endpoint implementation.
/// </summary>
public class ReplyToSendEndpoint :
    SendEndpointProxy
{
    readonly IDestination _destination;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="destination">The destination value.</param>
    public ReplyToSendEndpoint(ISendEndpoint endpoint, IDestination destination)
        : base(endpoint)
    {
        _destination = destination;
    }

    /// <summary>
    /// Gets pipe proxy.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
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
