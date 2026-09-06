using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Defines the operations required by request timeout expired.</summary>
/// <typeparam name="TRequest">The request type.</typeparam>
public interface RequestTimeoutExpired<out TRequest>
    where TRequest : class
{
    /// <summary>The correlationId of the state machine.</summary>
    Guid CorrelationId { get; }

    /// <summary>When the request expired.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>The expiration time that was scheduled for the request.</summary>
    DateTimeOffset ExpirationTime { get; }

    /// <summary>The requestId of the request.</summary>
    Guid RequestId { get; }

    /// <summary>Gets the message.</summary>
    TRequest? Message { get; }
}
