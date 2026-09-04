using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Provides a reply to send endpoint implementation.
/// </summary>
public class ReplyToSendEndpoint :
    SendEndpointProxy
{
    readonly string _queueName;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="endpoint">The endpoint value.</param>
    /// <param name="queueName">The queue name value.</param>
    public ReplyToSendEndpoint(ISendEndpoint endpoint, string queueName)
        : base(endpoint)
    {
        _queueName = queueName;
    }

    /// <summary>
    /// Gets pipe proxy.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="pipe">The pipe value.</param>
    /// <returns>The result of the operation.</returns>
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
