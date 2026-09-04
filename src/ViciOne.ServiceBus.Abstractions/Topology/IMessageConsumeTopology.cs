using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// The message-specific Consume topology, which may be configured or otherwise
/// setup for use with the Consume specification.
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(ITopologyPipeBuilder<ConsumeContext<TMessage>> builder);
}
