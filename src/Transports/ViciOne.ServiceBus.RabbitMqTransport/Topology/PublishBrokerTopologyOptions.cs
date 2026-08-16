namespace ViciOne.ServiceBus
{
    using System;


    [Flags]
    public enum PublishBrokerTopologyOptions
    {
        FlattenHierarchy = 0,
        MaintainHierarchy = 1
    }
}
