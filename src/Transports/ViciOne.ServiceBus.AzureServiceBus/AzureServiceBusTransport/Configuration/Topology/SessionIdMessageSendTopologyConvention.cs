using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.AzureServiceBus.Configuration;

/// <summary>
/// Provides a session id message send topology convention implementation.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public class SessionIdMessageSendTopologyConvention<TMessage> :
    ISessionIdMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    IMessageSessionIdFormatter<TMessage>? _formatter;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public SessionIdMessageSendTopologyConvention(ISessionIdFormatter? formatter)
    {
        if (formatter != null)
            SetFormatter(formatter);
    }

    /// <summary>
    /// Attempts to get message send topology.
    /// </summary>
    /// <param name="messageSendTopology">The message send topology value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
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

    /// <summary>
    /// Attempts to get message send topology convention.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="convention">The convention value.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    /// <summary>
    /// Sets formatter.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public void SetFormatter(ISessionIdFormatter formatter)
    {
        _formatter = new MessageSessionIdFormatter<TMessage>(formatter);
    }

    /// <summary>
    /// Sets formatter.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    public void SetFormatter(IMessageSessionIdFormatter<TMessage> formatter)
    {
        _formatter = formatter;
    }
}
