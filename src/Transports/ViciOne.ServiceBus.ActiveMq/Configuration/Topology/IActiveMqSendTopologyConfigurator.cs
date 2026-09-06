using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Configures ActiveMQ send, error, and dead-letter destination topology.</summary>
public interface IActiveMqSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IActiveMqSendTopology
{
    /// <summary>Sets the callback applied to generated error-queue settings.</summary>
    Action<IActiveMqQueueConfigurator> ConfigureErrorSettings { set; }

    /// <summary>Sets the callback applied to generated dead-letter queue settings.</summary>
    Action<IActiveMqQueueConfigurator> ConfigureDeadLetterSettings { set; }
}
