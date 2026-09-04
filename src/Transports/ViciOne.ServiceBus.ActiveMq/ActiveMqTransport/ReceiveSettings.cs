using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Specify the receive settings for a receive transport
/// </summary>
public interface ReceiveSettings :
    EntitySettings
{
    /// <summary>
    /// The number of unacknowledged messages to allow to be processed concurrently
    /// </summary>
    int PrefetchCount { get; }

    /// <summary>
    /// Gets the concurrent message limit value.
    /// </summary>
    int ConcurrentMessageLimit { get; }

    /// <summary>
    /// Gets the selector value.
    /// </summary>
    string? Selector { get; }

    /// <summary>
    /// Get the input address for the transport on the specified host
    /// </summary>
    Uri GetInputAddress(Uri hostAddress);
}
