namespace ViciOne.ServiceBus.Transports.Fabric;

/// <summary>Buffers in-memory messages and dispatches them to connected receivers.</summary>
/// <typeparam name="TMessage">The message envelope type stored by the queue.</typeparam>
internal interface IMessageQueue<TMessage> :
    IMessageSink<TMessage>
    where TMessage : class
{
    /// <summary>Gets the queue name.</summary>
    string Name { get; }

    /// <summary>Connects a receiver to the queue.</summary>
    /// <param name="receiver">The receiver to connect.</param>
    /// <returns>A handle that disconnects the registration.</returns>
    ITopologyHandle ConnectMessageReceiver(IMessageReceiver<TMessage> receiver);
}
