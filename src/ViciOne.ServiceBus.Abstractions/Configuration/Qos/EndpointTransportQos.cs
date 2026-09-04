using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Transport-level quality-of-service settings owned by a receive endpoint. These values affect every consumer attached
/// to the endpoint and therefore must never be treated as consumer-local concurrency controls.
/// </summary>
public sealed record EndpointTransportQos
{
    /// <summary>
    /// Gets or sets the prefetch count value.
    /// </summary>
    public int? PrefetchCount { get; init; }

    /// <summary>
    /// Gets or sets the concurrent delivery limit value.
    /// </summary>
    public int? ConcurrentDeliveryLimit { get; init; }

    /// <summary>
    /// Gets a value indicating whether this declaration changes transport QoS.
    /// </summary>
    public bool IsSpecified => PrefetchCount is not null || ConcurrentDeliveryLimit is not null;

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
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
