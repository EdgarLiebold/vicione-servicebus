using System;

namespace ViciOne.ServiceBus.Providers.Transports;

/// <summary>Configures sql send topology.</summary>
public interface ISqlSendTopologyConfigurator :
    ISendTopologyConfigurator,
    ISqlSendTopology
{
    /// <summary>Gets or sets the configure error settings.</summary>
    Action<ISqlQueueConfigurator> ConfigureErrorSettings { set; }
    /// <summary>Gets or sets the configure dead letter settings.</summary>
    Action<ISqlQueueConfigurator> ConfigureDeadLetterSettings { set; }
}
