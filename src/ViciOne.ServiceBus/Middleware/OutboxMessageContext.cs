using System;
using System.Collections.Generic;

#nullable enable
namespace ViciOne.ServiceBus.Middleware;

public interface OutboxMessageContext :
    MessageContext
{
    long SequenceNumber { get; }

    new Guid MessageId { get; }

    string ContentType { get; }

    string MessageType { get; }

    string Body { get; }

    IReadOnlyDictionary<string, object> Properties { get; }
}
