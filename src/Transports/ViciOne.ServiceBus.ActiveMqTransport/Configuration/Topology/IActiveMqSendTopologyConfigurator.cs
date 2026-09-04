using System;

namespace ViciOne.ServiceBus;

public interface IActiveMqSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IActiveMqSendTopology
{
    Action<IActiveMqQueueConfigurator> ConfigureErrorSettings { set; }

    Action<IActiveMqQueueConfigurator> ConfigureDeadLetterSettings { set; }
}
