using System;

namespace ViciOne.ServiceBus.Util.Scanning;

/// <summary>Carries the persisted record for assembly scan.</summary>
public class AssemblyScanRecord
{
    /// <summary>Exposes the load exception used by the containing type.</summary>
    public Exception? LoadException;
    /// <summary>Exposes the name used by the containing type.</summary>
    public string? Name;

    /// <summary>Returns the string representation of this instance.</summary>
    /// <returns>The converted string.</returns>
    public override string ToString()
    {
        return LoadException == null ? Name ?? "" : $"{Name} (Failed)";
    }
}
