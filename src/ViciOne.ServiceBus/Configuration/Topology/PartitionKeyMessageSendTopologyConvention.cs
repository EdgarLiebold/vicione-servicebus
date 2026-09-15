using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates partition-key send topology when a formatter is configured.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
sealed class PartitionKeyMessageSendTopologyConvention<TMessage> :
    IPartitionKeyMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    IMessagePartitionKeyFormatter<TMessage>? _formatter;

    /// <summary>Creates an unconfigured partition-key convention.</summary>
    public PartitionKeyMessageSendTopologyConvention()
    {
    }

    /// <summary>Creates a convention backed by a transport-neutral formatter.</summary>
    /// <param name="formatter">The formatter used to obtain partition keys.</param>
    public PartitionKeyMessageSendTopologyConvention(IPartitionKeyFormatter formatter)
    {
        SetFormatter(formatter);
    }

    /// <summary>Attempts to create topology that assigns a partition key.</summary>
    /// <param name="messageSendTopology">Receives the topology when a formatter is configured.</param>
    /// <returns><see langword="true" /> when a formatter is configured; otherwise, <see langword="false" />.</returns>
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

    /// <summary>Attempts to expose this convention for a compatible message contract.</summary>
    /// <typeparam name="T">The requested message contract type.</typeparam>
    /// <param name="convention">Receives this convention when the contract types match.</param>
    /// <returns><see langword="true" /> when the contract types match; otherwise, <see langword="false" />.</returns>
    public bool TryGetMessageSendTopologyConvention<T>([NotNullWhen(true)] out IMessageSendTopologyConvention<T>? convention)
        where T : class
    {
        convention = this as IMessageSendTopologyConvention<T>;

        return convention != null;
    }

    /// <summary>Sets a transport-neutral partition-key formatter.</summary>
    /// <param name="formatter">The formatter to adapt for this message contract.</param>
    public void SetFormatter(IPartitionKeyFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);

        _formatter = new MessagePartitionKeyFormatter<TMessage>(formatter);
    }

    /// <summary>Sets the partition-key formatter for this message contract.</summary>
    /// <param name="formatter">The message-specific formatter.</param>
    public void SetFormatter(IMessagePartitionKeyFormatter<TMessage> formatter)
    {
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }
}
