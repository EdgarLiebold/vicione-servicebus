using System;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>Configures Amazon SQS queue topology for send, error, and skipped-message destinations.</summary>
public interface IAmazonSqsSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IAmazonSqsSendTopology
{
    /// <summary>Sets the callback applied to generated error-queue settings.</summary>
    Action<IAmazonSqsQueueConfigurator>? ConfigureErrorSettings { set; }
    /// <summary>Sets the callback applied to generated skipped-message queue settings.</summary>
    Action<IAmazonSqsQueueConfigurator>? ConfigureDeadLetterSettings { set; }
}
