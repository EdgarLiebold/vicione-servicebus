using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Captures the response route and payload of a request whose saga processing remains pending.</summary>
public interface IRequestStarted
{
    /// <summary>Gets the correlation identifier of the saga that owns the request.</summary>
    Guid CorrelationId { get; }

    /// <summary>Gets the request identifier from the original request.</summary>
    Guid RequestId { get; }

    /// <summary>Gets the response address from the original request.</summary>
    Uri ResponseAddress { get; }

    /// <summary>Gets the fault address from the original request.</summary>
    Uri FaultAddress { get; }

    /// <summary>Gets the instant after which a response must be discarded, or <see langword="null" /> for no deadline.</summary>
    DateTimeOffset? ExpirationTime { get; }

    /// <summary>Gets the message-type identities implemented by <see cref="Payload" />.</summary>
    string[] PayloadType { get; }

    /// <summary>Gets the serialized original request.</summary>
    object Payload { get; }
}
