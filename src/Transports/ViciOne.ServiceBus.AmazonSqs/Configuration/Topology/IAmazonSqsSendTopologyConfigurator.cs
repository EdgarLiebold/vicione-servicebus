using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for amazon sqs send topology configurator.
/// </summary>
public interface IAmazonSqsSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IAmazonSqsSendTopology
{
    /// <summary>
    /// Gets or sets the configure error settings value.
    /// </summary>
    Action<IAmazonSqsQueueConfigurator>? ConfigureErrorSettings { set; }
    /// <summary>
    /// Gets or sets the configure dead letter settings value.
    /// </summary>
    Action<IAmazonSqsQueueConfigurator>? ConfigureDeadLetterSettings { set; }
}
