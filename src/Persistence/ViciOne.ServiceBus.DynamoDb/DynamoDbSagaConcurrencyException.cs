using System;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>Indicates that an Amazon DynamoDB conditional write rejected a conflicting saga insert, update, or delete.</summary>
public sealed class DynamoDbSagaConcurrencyException :
    ConcurrencyException
{
    /// <summary>Creates a concurrency exception for the conflicting saga identity.</summary>
    /// <param name="message">A description of the concurrency conflict.</param>
    /// <param name="sagaType">The saga state type involved in the conflict.</param>
    /// <param name="correlationId">The conflicting saga correlation identifier.</param>
    public DynamoDbSagaConcurrencyException(string message, Type sagaType, Guid correlationId)
        : base(message, sagaType, correlationId)
    {
    }

    /// <summary>Creates a concurrency exception that retains the rejected Amazon DynamoDB operation error.</summary>
    /// <param name="message">A description of the concurrency conflict.</param>
    /// <param name="sagaType">The saga state type involved in the conflict.</param>
    /// <param name="correlationId">The conflicting saga correlation identifier.</param>
    /// <param name="innerException">The conditional-write exception reported by Amazon DynamoDB.</param>
    public DynamoDbSagaConcurrencyException(string message, Type sagaType, Guid correlationId, Exception innerException)
        : base(message, sagaType, correlationId, innerException)
    {
    }
}
