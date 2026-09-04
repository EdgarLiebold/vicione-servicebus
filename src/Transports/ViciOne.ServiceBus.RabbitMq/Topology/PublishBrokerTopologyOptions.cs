using System;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>
/// Specifies the available publish broker topology options values.
/// </summary>
[Flags]
public enum PublishBrokerTopologyOptions
{
    /// <summary>
    /// Indicates flatten hierarchy.
    /// </summary>
    FlattenHierarchy = 0,
    /// <summary>
    /// Indicates maintain hierarchy.
    /// </summary>
    MaintainHierarchy = 1
}
