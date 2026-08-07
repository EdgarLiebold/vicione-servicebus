// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IServiceBusSendTopologyConfigurator :
        ISendTopologyConfigurator,
        IServiceBusSendTopology
    {
        Action<IServiceBusEntityConfigurator> ConfigureErrorSettings { set; }
        Action<IServiceBusEntityConfigurator> ConfigureDeadLetterSettings { set; }

        new IServiceBusMessageSendTopologyConfigurator<T> GetMessageTopology<T>()
            where T : class;
    }
}
