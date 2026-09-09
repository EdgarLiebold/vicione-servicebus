using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Describes the transport-independent metadata carried by a message.</summary>
public interface MessageContext
{
    /// <summary>Gets the identifier assigned to the logical message.</summary>
    Guid? MessageId { get; }

    /// <summary>Gets the request identifier for a request, response, or request fault.</summary>
    Guid? RequestId { get; }

    /// <summary>Gets the application correlation identifier associated with the message.</summary>
    Guid? CorrelationId { get; }

    /// <summary>Gets the identifier propagated across the complete message conversation.</summary>
    Guid? ConversationId { get; }

    /// <summary>Gets the correlation identifier of the message or saga that initiated this message.</summary>
    Guid? InitiatorId { get; }

    /// <summary>The expiration time of the message if it is not intended to last forever.</summary>
    DateTimeOffset? ExpirationTime { get; }

    /// <summary>The address of the message producer that sent the message.</summary>
    Uri? SourceAddress { get; }

    /// <summary>The destination address of the message.</summary>
    Uri? DestinationAddress { get; }

    /// <summary>The response address to which responses to the request should be sent.</summary>
    Uri? ResponseAddress { get; }

    /// <summary>The fault address to which fault events should be sent if the message consumer faults.</summary>
    Uri? FaultAddress { get; }

    /// <summary>Gets the time at which the message was originally sent.</summary>
    DateTimeOffset? SentTime { get; }

    /// <summary>Gets the application and service-bus headers carried with the message.</summary>
    Headers Headers { get; }

    /// <summary>Gets information about the host that produced the message.</summary>
    HostInfo Host { get; }
}
