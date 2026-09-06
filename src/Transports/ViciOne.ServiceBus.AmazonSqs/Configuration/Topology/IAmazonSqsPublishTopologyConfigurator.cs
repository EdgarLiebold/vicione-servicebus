using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures Amazon SNS publish topology for message types.</summary>
public interface IAmazonSqsPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IAmazonSqsPublishTopology
{
    /// <summary>Gets publish topology for a message type.</summary>
    /// <typeparam name="T">The published message type.</typeparam>
    /// <returns>The typed Amazon SNS publish-topology configurator.</returns>
    new IAmazonSqsMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>Gets publish topology for a runtime message type.</summary>
    /// <param name="messageType">The runtime type of the message contract.</param>
    /// <returns>The untyped Amazon SNS publish-topology configurator.</returns>
    new IAmazonSqsMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
