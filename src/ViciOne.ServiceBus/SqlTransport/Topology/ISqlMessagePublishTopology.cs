using System;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.SqlTransport.Topology;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>
/// Defines the contract for sql message publish topology.
/// </summary>
/// <typeparam name="TMessage">The t message type.</typeparam>
public interface ISqlMessagePublishTopology<TMessage> :
    IMessagePublishTopology<TMessage>,
    ISqlMessagePublishTopology
    where TMessage : class
{
    /// <summary>
    /// Gets the topic value.
    /// </summary>
    Topic Topic { get; }

    /// <summary>
    /// Gets send settings.
    /// </summary>
    /// <param name="hostAddress">The host address value.</param>
    /// <returns>The result of the operation.</returns>
    SendSettings GetSendSettings(Uri hostAddress);

    /// <summary>
    /// Gets broker topology.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    BrokerTopology GetBrokerTopology();
}


/// <summary>
/// Defines the contract for sql message publish topology.
/// </summary>
public interface ISqlMessagePublishTopology
{
    /// <summary>
    /// Apply the message topology to the builder, including any implemented types
    /// </summary>
    /// <param name="builder">The topology builder</param>
    void Apply(IPublishEndpointBrokerTopologyBuilder builder);
}
