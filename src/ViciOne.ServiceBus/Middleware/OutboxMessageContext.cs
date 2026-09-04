using System;
using System.Collections.Generic;

#nullable enable
namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Defines the contract for outbox message context.
/// </summary>
public interface OutboxMessageContext :
    MessageContext
{
    /// <summary>
    /// Gets the sequence number value.
    /// </summary>
    long SequenceNumber { get; }

    /// <summary>
    /// Gets the message id value.
    /// </summary>
    new Guid MessageId { get; }

    /// <summary>
    /// Gets the content type value.
    /// </summary>
    string ContentType { get; }

    /// <summary>
    /// Gets the message type value.
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// Gets the body value.
    /// </summary>
    string Body { get; }

    /// <summary>
    /// Gets the properties value.
    /// </summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
