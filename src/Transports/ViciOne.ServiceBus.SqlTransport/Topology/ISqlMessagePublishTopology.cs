using System;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Defines the operations required by sql message publish topology.</summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface ISqlMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    ISqlMessagePublishTopology
    where TMessage : class
{
    /// <summary>Gets the topic.</summary>
    Topic Topic { get; }

    /// <summary>Gets send settings.</summary>
    /// <param name="hostAddress">The host address.</param>
    /// <returns>The send settings.</returns>
    SendSettings GetSendSettings(Uri hostAddress);

    /// <summary>Gets broker topology.</summary>
    /// <returns>The broker topology.</returns>
    BrokerTopology GetBrokerTopology();
}


/// <summary>Defines the operations required by sql message publish topology.</summary>
public interface ISqlMessagePublishTopology
{
    /// <summary>Apply the message topology to the builder, including any implemented types.</summary>
    /// <param name="builder">The topology builder.</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
