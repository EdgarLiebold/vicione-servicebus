using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Confirms that a concurrency limiter applied an adjustment command.</summary>
public interface ConcurrencyLimitUpdated
{
    /// <summary>Gets the UTC time at which the adjustment was applied.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the optional identifier of the adjusted limiter.</summary>
    string? LimiterId { get; }

    /// <summary>Gets the concurrency limit that was applied.</summary>
    int ConcurrencyLimit { get; }
}
