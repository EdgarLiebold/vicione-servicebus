using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Serialization;

public interface MessageEnvelope
{
    string? MessageId { get; }
    string? RequestId { get; }
    string? CorrelationId { get; }
    string? ConversationId { get; }
    string? InitiatorId { get; }
    string? SourceAddress { get; }
    string? DestinationAddress { get; }
    string? ResponseAddress { get; }
    string? FaultAddress { get; }
    string[]? MessageType { get; }
    object? Message { get; }
    DateTimeOffset? ExpirationTime { get; }
    DateTimeOffset? SentTime { get; }
    Dictionary<string, object?>? Headers { get; }
    HostInfo? Host { get; }
}
