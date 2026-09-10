namespace ViciOne.ServiceBus.Configuration;

/// <summary>Provides the shared handler connector for a message contract.</summary>
/// <typeparam name="TMessage">The message contract handled by the shared connector.</typeparam>
public static class HandlerConnectorCache<TMessage>
    where TMessage : class
{
    static readonly IHandlerConnector<TMessage> Cached = new HandlerConnector<TMessage>();

    /// <summary>Gets the process-wide stateless connector for this message contract.</summary>
    public static IHandlerConnector<TMessage> Connector => Cached;
}
