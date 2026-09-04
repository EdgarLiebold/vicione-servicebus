using System;

namespace ViciOne.ServiceBus;

public interface IServiceBusSendTopologyConfigurator :
    ISendTopologyConfigurator,
    IServiceBusSendTopology
{
    Action<IServiceBusEntityConfigurator> ConfigureErrorSettings { set; }
    Action<IServiceBusEntityConfigurator> ConfigureDeadLetterSettings { set; }

    new IServiceBusMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
        where T : class;
}
