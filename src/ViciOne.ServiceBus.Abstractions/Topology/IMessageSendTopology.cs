using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// The message-specific send topology, which may be configured or otherwise
/// setup for use with the send specification.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageSendTopology<TMessage>
    where TMessage : class
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder);
}
