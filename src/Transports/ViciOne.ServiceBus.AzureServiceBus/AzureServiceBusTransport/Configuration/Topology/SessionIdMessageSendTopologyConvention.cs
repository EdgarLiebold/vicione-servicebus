using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>Creates send topology that assigns an Azure Service Bus session identifier for a message type.</summary>
/// <typeparam name="TMessage">The message type to format.</typeparam>
public class SessionIdMessageSendTopologyConvention<TMessage> :
    ISessionIdMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    IMessageSessionIdFormatter<TMessage>? _formatter;

    /// <summary>Initializes the convention from an optional untyped session-id formatter.</summary>
    /// <param name="formatter">The formatter to adapt, or <see langword="null"/> until one is configured.</param>
    public SessionIdMessageSendTopologyConvention(ISessionIdFormatter? formatter)
    {
        if (formatter != null)
            SetFormatter(formatter);
    }

    /// <summary>Creates session-id send topology when a formatter has been configured.</summary>
    /// <param name="messageSendTopology">The created topology, or <see langword="null"/> when no formatter is available.</param>
    /// <returns><see langword="true"/> when topology was created.</returns>
    public bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
    {
        if (_formatter != null)
        {
            messageSendTopology = new SetSessionIdMessageSendTopology<TMessage>(_formatter);
            return true;
        }

        messageSendTopology = null;
        return false;
    }

    /// <summary>Returns this convention when the requested message type is <typeparamref name="TMessage"/>.</summary>
    /// <typeparam name="T">The requested message type.</typeparam>
    /// <param name="convention">This convention viewed for <typeparamref name="T"/>, when compatible.</param>
    /// <returns><see langword="true"/> when the requested type matches.</returns>
    public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    /// <summary>Adapts an untyped session-id formatter to this message type.</summary>
    /// <param name="formatter">The formatter to adapt.</param>
    public void SetFormatter(ISessionIdFormatter formatter)
    {
        _formatter = new MessageSessionIdFormatter<TMessage>(formatter);
    }

    /// <summary>Sets the message-specific session-id formatter.</summary>
    /// <param name="formatter">The formatter to invoke for each message.</param>
    public void SetFormatter(IMessageSessionIdFormatter<TMessage> formatter)
    {
        _formatter = formatter;
    }
}
