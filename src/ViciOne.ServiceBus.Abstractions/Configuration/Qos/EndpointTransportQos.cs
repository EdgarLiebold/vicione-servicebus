using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Transport-level quality-of-service settings owned by a receive endpoint. These values affect every consumer attached
/// to the endpoint and therefore must never be treated as consumer-local concurrency controls.
/// </summary>
public sealed record EndpointTransportQos
{
    /// <summary>Gets the broker-specific number of messages fetched ahead of processing.</summary>
    public int? PrefetchCount { get; init; }

    /// <summary>Gets the maximum number of messages delivered concurrently to the endpoint.</summary>
    public int? ConcurrentDeliveryLimit { get; init; }

    /// <summary>Gets whether either transport quality-of-service setting is specified.</summary>
    public bool IsSpecified => PrefetchCount is not null || ConcurrentDeliveryLimit is not null;

    /// <summary>Validates that every specified transport limit is positive.</summary>
    /// <returns>This validated settings instance.</returns>
    public EndpointTransportQos Validate()
    {
        if (PrefetchCount is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(PrefetchCount),
                PrefetchCount,
                "PrefetchCount must be positive when specified.");
        }

        if (ConcurrentDeliveryLimit is <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ConcurrentDeliveryLimit),
                ConcurrentDeliveryLimit,
                "ConcurrentDeliveryLimit must be positive when specified.");
        }

        return this;
    }
}
