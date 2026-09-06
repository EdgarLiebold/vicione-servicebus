using System;

namespace ViciOne.ServiceBus.ActiveMq.Topology;

/// <summary>Controls whether publish topology preserves implemented-message hierarchy.</summary>
[Flags]
public enum PublishBrokerTopologyOptions
{
    /// <summary>Adds all publish topics to one flat topology.</summary>
    FlattenHierarchy = 0,
    /// <summary>Uses nested builder scopes for implemented message contracts.</summary>
    MaintainHierarchy = 1
}
