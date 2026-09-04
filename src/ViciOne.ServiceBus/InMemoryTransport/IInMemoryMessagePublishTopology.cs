using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for in memory message publish topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface IInMemoryMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopology
    where TMessage : class
{
    /// <summary>
    /// Gets the exchange type value.
    /// </summary>
    ExchangeType ExchangeType { get; }
}


/// <summary>
/// Defines the contract for in memory message publish topology.
/// </summary>
public interface IInMemoryMessagePublishTopology
{
    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    void Apply(IMessageFabricPublishTopologyBuilder builder);
}
