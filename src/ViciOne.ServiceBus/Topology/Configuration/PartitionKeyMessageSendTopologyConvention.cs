using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Applies conventions for partition key message send topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public class PartitionKeyMessageSendTopologyConvention<TMessage> :
    IPartitionKeyMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    IMessagePartitionKeyFormatter<TMessage>? _formatter;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="formatter">The formatter.</param>
    public PartitionKeyMessageSendTopologyConvention(IPartitionKeyFormatter? formatter)
    {
        if (formatter != null)
            SetFormatter(formatter);
    }

    /// <summary>Attempts to get message send topology.</summary>
    /// <param name="messageSendTopology">Receives the message send topology produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSendTopology([NotNullWhen(true)] out IMessageSendTopology<TMessage>? messageSendTopology)
    {
        if (_formatter != null)
        {
            messageSendTopology = new SetPartitionKeyMessageSendTopology<TMessage>(_formatter);
            return true;
        }

        messageSendTopology = null;
        return false;
    }

    /// <summary>Attempts to get message send topology convention.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="convention">Receives the convention produced by the operation.</param>
    /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    /// <summary>Sets formatter.</summary>
    /// <param name="formatter">The formatter.</param>
    public void SetFormatter(IPartitionKeyFormatter formatter)
    {
        _formatter = new MessagePartitionKeyFormatter<TMessage>(formatter);
    }

    /// <summary>Sets formatter.</summary>
    /// <param name="formatter">The formatter.</param>
    public void SetFormatter(IMessagePartitionKeyFormatter<TMessage> formatter)
    {
        _formatter = formatter;
    }
}
