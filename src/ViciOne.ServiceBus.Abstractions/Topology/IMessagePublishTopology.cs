using System;
using System.Diagnostics.CodeAnalysis;
using ViciOne.ServiceBus.Configuration;

namespace ViciOne.ServiceBus.Advanced.Topology;

/// <summary>
/// The message-specific publish topology, which may be configured or otherwise
/// setup for use with the publish specification.
/// </summary>
/// <typeparam name="TMessage">The message contract processed by the member.</typeparam>
public interface IMessagePublishTopology<TMessage> :
    IMessagePublishTopology
    where TMessage : class
{
    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    void Apply(ITopologyPipeBuilder<PublishContext<TMessage>> builder);
}


/// <summary>Defines the operations required by message publish topology.</summary>
public interface IMessagePublishTopology
{
    /// <summary>True if the message type should be excluded from the broker topology.</summary>
    bool Exclude { get; }

    /// <summary>
    /// Returns the publish address for the message, using the topology rules. This cannot use
    /// a PublishContext because the transport isn't available yet.
    /// </summary>
    /// <param name="baseAddress">The host base address, used to build out the exchange address.</param>
    /// <param name="publishAddress">The address where the publish endpoint should send the message.</param>
    /// <returns>true if the address was available, otherwise false.</returns>
    bool TryGetPublishAddress(Uri baseAddress, [NotNullWhen(true)] out Uri? publishAddress);
}
