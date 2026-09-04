using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>
/// Defines the contract for message envelope.
/// </summary>
public interface MessageEnvelope
{
    /// <summary>
    /// Gets the message id value.
    /// </summary>
    string? MessageId { get; }
    /// <summary>
    /// Gets the request id value.
    /// </summary>
    string? RequestId { get; }
    /// <summary>
    /// Gets the correlation id value.
    /// </summary>
    string? CorrelationId { get; }
    /// <summary>
    /// Gets the conversation id value.
    /// </summary>
    string? ConversationId { get; }
    /// <summary>
    /// Gets the initiator id value.
    /// </summary>
    string? InitiatorId { get; }
    /// <summary>
    /// Gets the source address value.
    /// </summary>
    string? SourceAddress { get; }
    /// <summary>
    /// Gets the destination address value.
    /// </summary>
    string? DestinationAddress { get; }
    /// <summary>
    /// Gets the response address value.
    /// </summary>
    string? ResponseAddress { get; }
    /// <summary>
    /// Gets the fault address value.
    /// </summary>
    string? FaultAddress { get; }
    /// <summary>
    /// Gets the message type value.
    /// </summary>
    string[]? MessageType { get; }
    /// <summary>
    /// Gets the message value.
    /// </summary>
    object? Message { get; }
    /// <summary>
    /// Gets the expiration time value.
    /// </summary>
    DateTimeOffset? ExpirationTime { get; }
    /// <summary>
    /// Gets the sent time value.
    /// </summary>
    DateTimeOffset? SentTime { get; }
    /// <summary>
    /// Gets the headers value.
    /// </summary>
    Dictionary<string, object?>? Headers { get; }
    /// <summary>
    /// Gets the host value.
    /// </summary>
    HostInfo? Host { get; }
}
