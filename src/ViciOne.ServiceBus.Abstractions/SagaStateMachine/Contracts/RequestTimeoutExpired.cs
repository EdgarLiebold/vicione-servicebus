using System;

namespace ViciOne.ServiceBus.Contracts;

public interface RequestTimeoutExpired<out TRequest>
    where TRequest : class
{
    /// <summary>
    /// The correlationId of the state machine
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// When the request expired
    /// </summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>
    /// The expiration time that was scheduled for the request
    /// </summary>
    DateTimeOffset ExpirationTime { get; }

    /// <summary>
    /// The requestId of the request
    /// </summary>
    Guid RequestId { get; }

    /// <summary>
    /// The  original request message.
    /// </summary>
    TRequest? Message { get; }
}
