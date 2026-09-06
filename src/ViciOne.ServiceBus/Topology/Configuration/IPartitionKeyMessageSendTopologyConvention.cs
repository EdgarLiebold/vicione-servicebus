using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Defines the operations required by partition key message send topology convention.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IPartitionKeyMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>Sets formatter.</summary>
    /// <param name="formatter">The formatter.</param>
    void SetFormatter(IPartitionKeyFormatter formatter);
    /// <summary>Sets formatter.</summary>
    /// <param name="formatter">The formatter.</param>
    void SetFormatter(IMessagePartitionKeyFormatter<TMessage> formatter);
}
