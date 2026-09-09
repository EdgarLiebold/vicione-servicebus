using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Requests a new concurrency limit from one or more matching limiters.</summary>
public interface SetConcurrencyLimit
{
    /// <summary>Gets the command timestamp used for ordering, or <see langword="null" /> to use the transport sent time.</summary>
    DateTimeOffset? Timestamp { get; }

    /// <summary>Gets the optional case-insensitive limiter identifier; <see langword="null" /> targets every limiter.</summary>
    string? LimiterId { get; }

    /// <summary>Gets the positive concurrency limit to apply.</summary>
    int ConcurrencyLimit { get; }
}
