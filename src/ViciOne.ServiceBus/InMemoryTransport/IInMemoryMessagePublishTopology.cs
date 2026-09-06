using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Transports.Fabric;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by in memory message publish topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IInMemoryMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    IInMemoryMessagePublishTopology
    where TMessage : class
{
    /// <summary>Gets the exchange type.</summary>
    ExchangeType ExchangeType { get; }
}


/// <summary>Defines the operations required by in memory message publish topology.</summary>
public interface IInMemoryMessagePublishTopology
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(IMessageFabricPublishTopologyBuilder builder);
}
