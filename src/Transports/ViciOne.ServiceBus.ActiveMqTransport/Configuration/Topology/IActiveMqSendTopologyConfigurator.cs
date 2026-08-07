// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface IActiveMqSendTopologyConfigurator :
        ISendTopologyConfigurator,
        IActiveMqSendTopology
    {
        Action<IActiveMqQueueConfigurator> ConfigureErrorSettings { set; }

        Action<IActiveMqQueueConfigurator> ConfigureDeadLetterSettings { set; }
    }
}
