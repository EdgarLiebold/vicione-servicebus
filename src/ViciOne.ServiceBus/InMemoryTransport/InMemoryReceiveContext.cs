using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.InMemoryTransport;

/// <summary>
/// Provides an in memory receive context implementation.
/// </summary>
public sealed class InMemoryReceiveContext :
    BaseReceiveContext,
    RoutingKeyConsumeContext
{
    readonly MessageBody _body;
    readonly InMemoryTransportMessage _message;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="receiveEndpointContext">The receive endpoint context value.</param>
    public InMemoryReceiveContext(InMemoryTransportMessage message, InMemoryReceiveEndpointContext receiveEndpointContext)
        : base(message.DeliveryCount > 0, receiveEndpointContext, GetPayloads(message))
    {
        _message = message;

        _body = new BytesMessageBody(message.Body);
    }

    /// <summary>
    /// Gets the header provider value.
    /// </summary>
    protected override IHeaderProvider HeaderProvider => new DictionarySendHeaderProvider(_message.Headers);

    /// <summary>
    /// Gets the body value.
    /// </summary>
    public override MessageBody Body => EnforceMessageLimits(_body);
    /// <summary>
    /// Gets the routing key value.
    /// </summary>
    public string? RoutingKey => _message.RoutingKey;

    static object[] GetPayloads(InMemoryTransportMessage message)
        => message.DurableSendContext is { } durableSendContext ? [durableSendContext] : [];
}
