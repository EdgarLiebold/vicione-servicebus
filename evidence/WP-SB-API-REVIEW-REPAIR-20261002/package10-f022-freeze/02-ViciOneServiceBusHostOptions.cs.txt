using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures how the application host starts and stops registered bus instances.</summary>
public sealed class ViciOneServiceBusHostOptions
{
    /// <summary>Gets or sets whether host startup waits until every registered bus instance has started.</summary>
    public bool WaitUntilStarted { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration of bus startup. A <see langword="null"/> value applies no timeout in addition to caller cancellation.
    /// </summary>
    /// <remarks>When specified, the duration is positive and its truncated whole milliseconds are at most 4294967294.</remarks>
    public TimeSpan? StartTimeout { get; set; }

    /// <summary>
    /// Gets or sets the maximum duration of bus shutdown. A <see langword="null"/> value applies no timeout in addition to caller cancellation.
    /// </summary>
    /// <remarks>When specified, the duration is positive and its truncated whole milliseconds are at most 4294967294.</remarks>
    public TimeSpan? StopTimeout { get; set; }

    /// <summary>
    /// Gets or sets how long consumers may finish active work before their <see cref="PipeContext.CancellationToken"/> is canceled.
    /// A <see langword="null"/> value allows consumers to use the complete bus shutdown interval.
    /// </summary>
    /// <remarks>When specified, the duration is positive and its truncated whole milliseconds are at most 4294967294.</remarks>
    public TimeSpan? ConsumerStopTimeout { get; set; }
}
