using System;

namespace ViciOne.ServiceBus.DynamoDb;

/// <summary>
/// Represents an error related to dynamo db saga concurrency.
/// </summary>
[Serializable]
public class DynamoDbSagaConcurrencyException :
    ConcurrencyException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="sagaType">The saga type value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    public DynamoDbSagaConcurrencyException(string message, Type sagaType, Guid correlationId)
        : base(message, sagaType, correlationId)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="sagaType">The saga type value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public DynamoDbSagaConcurrencyException(string message, Type sagaType, Guid correlationId, Exception innerException)
        : base(message, sagaType, correlationId, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public DynamoDbSagaConcurrencyException()
    {
    }
}
