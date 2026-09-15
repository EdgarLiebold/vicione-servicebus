using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates routing-key send topology when a formatter is configured.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
sealed class RoutingKeyMessageSendTopologyConvention<TMessage> :
    IRoutingKeyMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    IMessageRoutingKeyFormatter<TMessage>? _formatter;

    /// <summary>Creates an unconfigured routing-key convention.</summary>
    public RoutingKeyMessageSendTopologyConvention()
    {
    }

    /// <summary>Creates a convention backed by a transport-neutral formatter.</summary>
    /// <param name="formatter">The formatter used to obtain routing keys.</param>
    public RoutingKeyMessageSendTopologyConvention(IRoutingKeyFormatter formatter)
    {
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

    /// <summary>Sets a transport-neutral routing-key formatter.</summary>
    /// <param name="formatter">The formatter to adapt for this message contract.</param>
    public void SetFormatter(IRoutingKeyFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        _formatter = new MessageRoutingKeyFormatter<TMessage>(formatter);
    }

    /// <summary>Sets the routing-key formatter for this message contract.</summary>
    /// <param name="formatter">The message-specific formatter.</param>
    public void SetFormatter(IMessageRoutingKeyFormatter<TMessage> formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }
}
