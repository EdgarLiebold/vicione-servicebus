using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Configures how a message type obtains its Azure Service Bus session identifier.</summary>
/// <typeparam name="TMessage">The message type to format.</typeparam>
public interface ISessionIdMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>Sets an untyped session-id formatter for the message type.</summary>
    /// <param name="formatter">The formatter to adapt to <typeparamref name="TMessage"/>.</param>
    void SetFormatter(ISessionIdFormatter formatter);
    /// <summary>Sets a message-specific session-id formatter.</summary>
    /// <param name="formatter">The formatter invoked for <typeparamref name="TMessage"/>.</param>
    void SetFormatter(IMessageSessionIdFormatter<TMessage> formatter);
}
