using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.InMemoryTransport.Topology;

/// <summary>Exposes and applies message-specific in-memory consume topology.</summary>
internal interface IInMemoryConsumeTopology :
    IConsumeTopology
{
    /// <summary>Gets consume topology for a message contract.</summary>
    /// <typeparam name="TMessage">The message contract.</typeparam>
    /// <returns>The message-specific consume topology.</returns>
    new IInMemoryMessageConsumeTopology<TMessage> GetMessageTopology<TMessage>()
        where TMessage : class;

    /// <summary>Applies all configured exchange bindings to a consume topology builder.</summary>
    /// <param name="builder">The consume topology builder to update.</param>
    void Apply(IMessageFabricConsumeTopologyBuilder builder);
}
