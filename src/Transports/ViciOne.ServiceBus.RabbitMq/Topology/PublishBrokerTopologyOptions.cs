using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Controls how implemented-message exchange hierarchies are represented in RabbitMQ.</summary>
[Flags]
public enum PublishBrokerTopologyOptions
{
    /// <summary>Declares implemented message contracts without preserving parent-child exchange bindings.</summary>
    FlattenHierarchy = 0,
    /// <summary>Preserves parent-child exchange bindings for directly implemented message contracts.</summary>
    MaintainHierarchy = 1
}
