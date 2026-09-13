using System;

namespace ViciOne.ServiceBus.Contracts;

/// <summary>Reports that a saga-owned request faulted and carries the serialized fault.</summary>
public interface IRequestFaulted
{
    /// <summary>Gets the correlation identifier of the saga that owns the request.</summary>
    Guid CorrelationId { get; }

    /// <summary>Gets the message-type identities implemented by <see cref="Payload" />.</summary>
    string[] PayloadType { get; }

    /// <summary>Gets the serialized request fault.</summary>
    object Payload { get; }
}
