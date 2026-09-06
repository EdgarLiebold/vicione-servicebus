using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Defines the operations required by message envelope.</summary>
public interface MessageEnvelope
{
    /// <summary>Gets the message id.</summary>
    string? MessageId { get; }
    /// <summary>Gets the request id.</summary>
    string? RequestId { get; }
    /// <summary>Gets the correlation id.</summary>
    string? CorrelationId { get; }
    /// <summary>Gets the conversation id.</summary>
    string? ConversationId { get; }
    /// <summary>Gets the initiator id.</summary>
    string? InitiatorId { get; }
    /// <summary>Gets the source address.</summary>
    string? SourceAddress { get; }
    /// <summary>Gets the destination address.</summary>
    string? DestinationAddress { get; }
    /// <summary>Gets the response address.</summary>
    string? ResponseAddress { get; }
    /// <summary>Gets the fault address.</summary>
    string? FaultAddress { get; }
    /// <summary>Gets the message type.</summary>
    string[]? MessageType { get; }
    /// <summary>Gets the message.</summary>
    object? Message { get; }
    /// <summary>Gets the expiration time.</summary>
    DateTimeOffset? ExpirationTime { get; }
    /// <summary>Gets the sent time.</summary>
    DateTimeOffset? SentTime { get; }
    /// <summary>Gets the headers.</summary>
    Dictionary<string, object?>? Headers { get; }
    /// <summary>Gets the host.</summary>
    HostInfo? Host { get; }
}
