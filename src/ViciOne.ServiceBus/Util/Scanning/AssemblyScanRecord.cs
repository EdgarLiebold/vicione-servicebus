using System;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>
/// Provides an assembly scan record implementation.
/// </summary>
public class AssemblyScanRecord
{
    /// <summary>
    /// Defines the load exception value.
    /// </summary>
    public Exception? LoadException;
    /// <summary>
    /// Defines the name value.
    /// </summary>
    public string? Name;

    /// <summary>
    /// Returns the string representation of this instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public override string ToString()
    {
        return LoadException == null ? Name ?? "" : $"{Name} (Failed)";
    }
}
