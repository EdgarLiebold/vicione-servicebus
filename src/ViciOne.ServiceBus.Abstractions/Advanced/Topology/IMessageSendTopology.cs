using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Applies message-specific topology to a send pipeline.</summary>
/// <typeparam name="TMessage">The sent message contract.</typeparam>
public interface IMessageSendTopology<TMessage>
    where TMessage : class
{
    /// <summary>Applies message-specific send topology to a send-pipe builder.</summary>
    /// <param name="builder">The send-pipe topology builder.</param>
    void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder);
}
