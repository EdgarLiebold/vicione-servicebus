// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;


    public interface ISqlSendTopologyConfigurator :
        ISendTopologyConfigurator,
        ISqlSendTopology
    {
        Action<ISqlQueueConfigurator> ConfigureErrorSettings { set; }
        Action<ISqlQueueConfigurator> ConfigureDeadLetterSettings { set; }
    }
}
