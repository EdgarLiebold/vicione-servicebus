using System;

namespace ViciOne.ServiceBus;

[Flags]
public enum PublishBrokerTopologyOptions
{
    FlattenHierarchy = 0,
    MaintainHierarchy = 1
}
