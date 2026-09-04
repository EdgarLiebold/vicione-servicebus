using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs publish topology configurator.
/// </summary>
public interface IAmazonSqsPublishTopologyConfigurator :
    IPublishTopologyConfigurator,
    IAmazonSqsPublishTopology
{
    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    new IAmazonSqsMessagePublishTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;

    /// <summary>
    /// Gets message topology.
    /// </summary>
    /// <param name="messageType">The message type value.</param>
    /// <returns>The result of the operation.</returns>
    new IAmazonSqsMessagePublishTopologyConfigurator GetMessageTopology(Type messageType);
}
