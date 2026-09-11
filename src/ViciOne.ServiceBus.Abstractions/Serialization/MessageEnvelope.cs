using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Serialization;

/// <summary>Provides serializer-independent access to a message wire envelope and its metadata.</summary>
public interface MessageEnvelope
{
    /// <summary>Gets the message identifier in GUID text form.</summary>
    string? MessageId { get; }
    /// <summary>Gets the request identifier in GUID text form.</summary>
    string? RequestId { get; }
    /// <summary>Gets the correlation identifier in GUID text form.</summary>
    string? CorrelationId { get; }
    /// <summary>Gets the conversation identifier in GUID text form.</summary>
    string? ConversationId { get; }
    /// <summary>Gets the initiating message identifier in GUID text form.</summary>
    string? InitiatorId { get; }
    /// <summary>Gets the absolute source endpoint address.</summary>
    string? SourceAddress { get; }
    /// <summary>Gets the absolute destination endpoint address.</summary>
    string? DestinationAddress { get; }
    /// <summary>Gets the absolute response endpoint address.</summary>
    string? ResponseAddress { get; }
    /// <summary>Gets the absolute fault endpoint address.</summary>
    string? FaultAddress { get; }
    /// <summary>Gets the ordered message contract URNs.</summary>
    IReadOnlyList<string>? MessageTypes { get; }
    /// <summary>Gets the serializer-specific application message representation.</summary>
    object? Message { get; }
    /// <summary>Gets the absolute message expiration time.</summary>
    DateTimeOffset? ExpirationTime { get; }
    /// <summary>Gets the time at which the envelope was created.</summary>
    DateTimeOffset? SentTime { get; }
    /// <summary>Gets the application headers.</summary>
    IReadOnlyDictionary<string, object?>? Headers { get; }
    /// <summary>Gets metadata that identifies the sending host.</summary>
    HostInfo? Host { get; }
}
