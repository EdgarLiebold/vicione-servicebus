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

    /// <summary>Gets the absolute expiration time, or <see langword="null" /> when the message does not expire.</summary>
    DateTimeOffset? ExpirationTime { get; }

    /// <summary>Gets the address of the endpoint that produced the message.</summary>
    Uri? SourceAddress { get; }

    /// <summary>Gets the address to which the message was sent.</summary>
    Uri? DestinationAddress { get; }

    /// <summary>Gets the address to which responses should be sent.</summary>
    Uri? ResponseAddress { get; }

    /// <summary>Gets the address to which consumer fault messages should be sent.</summary>
    Uri? FaultAddress { get; }

    /// <summary>Gets the time at which the message was originally sent.</summary>
    DateTimeOffset? SentTime { get; }

    /// <summary>Gets the application and service-bus headers carried with the message.</summary>
    Headers Headers { get; }

    /// <summary>Gets information about the host that produced the message.</summary>
    HostInfo Host { get; }
}
