using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Advanced.Serialization;

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

// Kept separate from the public outbox contract: in-memory and legacy outboxes do not carry a durable proof.
internal interface IDurableOutboxMessageContext
{
    DurablePayloadAdmissionProof? AdmissionProof { get; }
}
