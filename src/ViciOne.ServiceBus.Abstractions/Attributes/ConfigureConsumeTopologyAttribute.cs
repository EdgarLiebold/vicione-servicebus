using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Specify whether the message type should be used to configure the broker topology for the consumer.
/// if configured. Types will this attribute will not have their matching topic/exchange bound to the
/// receive endpoint queue.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
public class ConfigureConsumeTopologyAttribute :
    Attribute
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConfigureConsumeTopologyAttribute()
    {
        ConfigureConsumeTopology = true;
    }

    /// <param name="configureConsumeTopology">When false, the consume topology will not be configured</param>
    public ConfigureConsumeTopologyAttribute(bool configureConsumeTopology)
    {
        ConfigureConsumeTopology = configureConsumeTopology;
    }

    /// <summary>
    /// Gets the configure consume topology value.
    /// </summary>
    public bool ConfigureConsumeTopology { get; }
}
