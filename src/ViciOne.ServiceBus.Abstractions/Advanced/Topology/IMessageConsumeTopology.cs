using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>Applies message-specific consume topology to a receive pipeline.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public interface IMessageConsumeTopology<TMessage>
    where TMessage : class
{
    /// <summary>Applies message-specific consume topology to a receive-pipe builder.</summary>
    /// <param name="builder">The receive-pipe topology builder.</param>
    void Apply(ITopologyPipeBuilder<ConsumeContext<TMessage>> builder);
}
