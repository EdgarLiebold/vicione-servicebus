using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Exposes state for outbox message operations.</summary>
public interface OutboxMessageContext :
    MessageContext
{
    /// <summary>Gets the sequence number.</summary>
    long SequenceNumber { get; }

    /// <summary>Gets the message id.</summary>
    new Guid MessageId { get; }

    /// <summary>Gets the content type.</summary>
    string ContentType { get; }

    /// <summary>Gets the message type.</summary>
    string MessageType { get; }

    /// <summary>Gets the body.</summary>
    string Body { get; }

    /// <summary>Gets the properties.</summary>
    IReadOnlyDictionary<string, object> Properties { get; }
}
