using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql send topology.</summary>
public interface ISqlSendTopologyConfigurator :
    ISendTopologyConfigurator,
    ISqlSendTopology
{
    /// <summary>Sets the callback that configures the error queue.</summary>
    Action<ISqlQueueConfigurator> ConfigureErrorSettings { set; }
    /// <summary>Sets the callback that configures the dead-letter queue.</summary>
    Action<ISqlQueueConfigurator> ConfigureDeadLetterSettings { set; }
}
