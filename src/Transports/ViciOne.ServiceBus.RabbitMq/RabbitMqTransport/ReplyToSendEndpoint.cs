using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Routes a response through RabbitMQ direct reply-to using the request's opaque reply routing key.</summary>
public class ReplyToSendEndpoint :
    SendEndpointProxy
{
    readonly string _queueName;

    /// <summary>Creates a proxy that adds the request's reply routing key to typed responses.</summary>
    /// <param name="endpoint">The underlying direct-reply-to send endpoint.</param>
    /// <param name="queueName">The opaque reply routing key carried by the request.</param>
    public ReplyToSendEndpoint(ISendEndpoint endpoint, string queueName)
        : base(endpoint)
    {
        _queueName = queueName;
    }

    /// <summary>Wraps a typed send pipeline with direct-reply-to routing-key assignment.</summary>
    /// <typeparam name="T">The response message contract.</typeparam>
    /// <param name="pipe">The caller-supplied send pipeline.</param>
    /// <returns>The direct-reply-to pipeline proxy.</returns>
    protected override IPipe<SendContext<T>> GetPipeProxy<T>(IPipe<SendContext<T>>? pipe = default)
    {
        return new ReplyToPipe<T>(_queueName, pipe);
    }


    class ReplyToPipe<TMessage> :
        SendContextPipeAdapter<TMessage>
        where TMessage : class
    {
        readonly string _queueName;

        public ReplyToPipe(string queueName, IPipe<SendContext<TMessage>>? pipe)
            : base(pipe)
        {
            _queueName = queueName;
        }

        protected override void Send(SendContext<TMessage> context)
        {
            context.SetRoutingKey(_queueName);
        }

        protected override void Send<T>(SendContext<T> context)
        {
        }
    }
}
