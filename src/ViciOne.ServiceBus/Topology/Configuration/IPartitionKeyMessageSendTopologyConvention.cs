using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for partition key message send topology convention.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IPartitionKeyMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Sets formatter.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    void SetFormatter(IPartitionKeyFormatter formatter);
    /// <summary>
    /// Sets formatter.
    /// </summary>
    /// <param name="formatter">The formatter value.</param>
    void SetFormatter(IMessagePartitionKeyFormatter<TMessage> formatter);
}
