using System;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Defines ActiveMQ queue, selector, prefetch, and concurrency settings for a receive transport.</summary>
public interface ReceiveSettings :
    EntitySettings
{
    /// <summary>Gets the maximum number of unacknowledged messages prefetched from the broker.</summary>
    int PrefetchCount { get; }

    /// <summary>Gets the maximum number of messages processed concurrently.</summary>
    int ConcurrentMessageLimit { get; }

    /// <summary>Gets the optional Apache NMS message selector.</summary>
    string? Selector { get; }

    /// <summary>Builds the receive transport's input address against a broker host.</summary>
    /// <param name="hostAddress">The configured broker address.</param>
    /// <returns>The absolute queue input address.</returns>
    Uri GetInputAddress(Uri hostAddress);
}
