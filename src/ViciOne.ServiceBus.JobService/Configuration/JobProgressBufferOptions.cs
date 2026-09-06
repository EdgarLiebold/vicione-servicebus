using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures when buffered job-progress updates are published to the coordinator.</summary>
public sealed class JobProgressBufferOptions
{
    /// <summary>Gets or sets the maximum number of updates coalesced before publishing the latest value.</summary>
    public int UpdateLimit { get; set; } = 1000;

    /// <summary>Gets or sets the maximum time an available progress update may remain buffered.</summary>
    public TimeSpan TimeLimit { get; set; } = TimeSpan.FromSeconds(30);
}
