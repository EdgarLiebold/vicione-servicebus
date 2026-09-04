using System;

namespace ViciOne.ServiceBus.ActiveMqTransport.Topology;

[Flags]
public enum PublishBrokerTopologyOptions
{
    FlattenHierarchy = 0,
    MaintainHierarchy = 1
}
