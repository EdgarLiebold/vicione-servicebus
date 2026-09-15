using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures routing keys for one sent message contract.</summary>
/// <typeparam name="TMessage">The sent message contract type.</typeparam>
public interface IRoutingKeyMessageSendTopologyConvention<TMessage> :
    IMessageSendTopologyConvention<TMessage>
    where TMessage : class
{
    /// <summary>Sets a transport-neutral routing-key formatter.</summary>
    /// <param name="formatter">The formatter to adapt for this message contract.</param>
    void SetFormatter(IRoutingKeyFormatter formatter);
    /// <summary>Sets the routing-key formatter for this message contract.</summary>
    /// <param name="formatter">The message-specific formatter.</param>
    void SetFormatter(IMessageRoutingKeyFormatter<TMessage> formatter);
}
