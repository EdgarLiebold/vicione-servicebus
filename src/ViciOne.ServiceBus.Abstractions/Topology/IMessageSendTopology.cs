using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// The message-specific send topology, which may be configured or otherwise
/// setup for use with the send specification.
/// </summary>
/// <typeparam name="TMessage"></typeparam>
public interface IMessageSendTopology<TMessage>
    where TMessage : class
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(ITopologyPipeBuilder<SendContext<TMessage>> builder);
}
