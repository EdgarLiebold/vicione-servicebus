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
