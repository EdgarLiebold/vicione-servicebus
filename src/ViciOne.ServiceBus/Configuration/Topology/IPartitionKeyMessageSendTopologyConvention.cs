using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures partition keys for one sent message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
public interface IPartitionKeyMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>Sets a transport-neutral partition-key formatter.</summary>
    /// <param name="formatter">The formatter to adapt for this message contract.</param>
    void SetFormatter(IPartitionKeyFormatter formatter);
    /// <summary>Sets the partition-key formatter for this message contract.</summary>
    /// <param name="formatter">The message-specific formatter.</param>
    void SetFormatter(IMessagePartitionKeyFormatter<TMessage> formatter);
}
