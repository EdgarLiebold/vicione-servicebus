using System;

namespace ViciOne.ServiceBus;

public interface ISqlSendTopologyConfigurator :
    ISendTopologyConfigurator,
    ISqlSendTopology
{
    Action<ISqlQueueConfigurator> ConfigureErrorSettings { set; }
    Action<ISqlQueueConfigurator> ConfigureDeadLetterSettings { set; }
}
