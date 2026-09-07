using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Controls whether the annotated message contract contributes broker bindings to a receive endpoint.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = true)]
public sealed class ConfigureConsumeTopologyAttribute :
    Attribute
{
    /// <summary>Initializes the attribute with consume-topology configuration enabled.</summary>
    public ConfigureConsumeTopologyAttribute()
        : this(true)
    {
    }

    /// <summary>Initializes the attribute with the specified consume-topology decision.</summary>
    /// <param name="configureConsumeTopology"><see langword="true"/> to configure broker bindings; otherwise, <see langword="false"/>.</param>
    public ConfigureConsumeTopologyAttribute(bool configureConsumeTopology)
    {
        ConfigureConsumeTopology = configureConsumeTopology;
    }

    /// <summary>Gets whether the annotated message contract contributes broker bindings.</summary>
    public bool ConfigureConsumeTopology { get; }
}
