using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Signals that a saga-owned request reached its configured deadline.</summary>
/// <typeparam name="TRequest">The type of request that expired.</typeparam>
public interface IRequestTimeoutExpired<out TRequest>
    where TRequest : class
{
    /// <summary>Gets the correlation identifier of the state-machine instance.</summary>
    Guid CorrelationId { get; }

    /// <summary>Gets the instant at which expiration was reported.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the deadline originally scheduled for the request.</summary>
    DateTimeOffset ExpirationTime { get; }

    /// <summary>Gets the identifier of the expired request.</summary>
    Guid RequestId { get; }

    /// <summary>Gets the expired request payload when it was retained.</summary>
    TRequest? Message { get; }
}
