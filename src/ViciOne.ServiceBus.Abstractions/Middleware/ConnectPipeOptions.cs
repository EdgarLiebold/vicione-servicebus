using System;

namespace ViciOne.ServiceBus.Advanced.Middleware;

/// <summary>
/// Specifies the available connect pipe options values.
/// </summary>
[Flags]
public enum ConnectPipeOptions
{
    /// <summary>
    /// Indicates configure consume topology.
    /// </summary>
    ConfigureConsumeTopology = 1,

    /// <summary>
    /// Indicates all.
    /// </summary>
    All = ConfigureConsumeTopology
}
