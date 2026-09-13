using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Reports that a saga-owned request completed and carries the serialized response.</summary>
public interface IRequestCompleted
{
    /// <summary>Gets the correlation identifier of the saga that owns the request.</summary>
    Guid CorrelationId { get; }

    /// <summary>Gets the instant at which the response was recorded.</summary>
    DateTimeOffset Timestamp { get; }

    /// <summary>Gets the message-type identities implemented by <see cref="Payload" />.</summary>
    string[] PayloadType { get; }

    /// <summary>Gets the serialized response payload.</summary>
    object Payload { get; }
}
