using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// The message-specific Consume topology, which may be configured or otherwise
/// setup for use with the Consume specification.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(ITopologyPipeBuilder<ConsumeContext<TMessage>> builder);
}
