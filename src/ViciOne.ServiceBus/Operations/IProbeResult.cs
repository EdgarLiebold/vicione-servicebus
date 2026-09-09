using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Operations;

/// <summary>Describes a structurally read-only snapshot of a service-bus diagnostic probe.</summary>
public interface IProbeResult
{
    /// <summary>Gets the unique identifier of this result.</summary>
    Guid ResultId { get; }

    /// <summary>Gets the identifier of the probe request.</summary>
    Guid ProbeId { get; }

    /// <summary>Gets the UTC timestamp at which the probe started.</summary>
    DateTimeOffset StartTimestamp { get; }

    /// <summary>Gets the elapsed time required to collect the snapshot.</summary>
    TimeSpan Duration { get; }

    /// <summary>Gets metadata for the host that produced the snapshot.</summary>
    HostInfo Host { get; }

    /// <summary>Gets the read-only root of the collected diagnostic structure.</summary>
    IReadOnlyDictionary<string, object> Results { get; }
}
