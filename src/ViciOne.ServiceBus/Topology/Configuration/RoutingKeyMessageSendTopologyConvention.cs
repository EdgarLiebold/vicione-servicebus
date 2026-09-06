using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies conventions for routing key message send topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class RoutingKeyMessageSendTopologyConvention<TMessage> :
    IRoutingKeyMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    IMessageRoutingKeyFormatter<TMessage>? _formatter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="formatter">The formatter.</param>
    public RoutingKeyMessageSendTopologyConvention(IRoutingKeyFormatter? formatter)
    {
        if (formatter != null)
            SetFormatter(formatter);
    }

    bool IMessageSendTopologyConvention<TMessage>.TryGetMessageSendTopology(
        [NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
    {
        if (_formatter != null)
        {
            messageSendTopology = new SetRoutingKeyMessageSendTopology<TMessage>(_formatter);
            return true;
        }

        messageSendTopology = null;
        return false;
    }

    bool IMessageSendTopologyConvention.TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    /// <summary>Sets formatter.</summary>
    /// <param name="formatter">The formatter.</param>
    public void SetFormatter(IRoutingKeyFormatter formatter)
    {
        _formatter = new MessageRoutingKeyFormatter<TMessage>(formatter);
    }

    /// <summary>Sets formatter.</summary>
    /// <param name="formatter">The formatter.</param>
    public void SetFormatter(IMessageRoutingKeyFormatter<TMessage> formatter)
    {
        _formatter = formatter;
    }
}
