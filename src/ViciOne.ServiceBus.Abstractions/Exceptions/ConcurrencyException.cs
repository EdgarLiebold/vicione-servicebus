using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to concurrency.
/// </summary>
[Serializable]
public class ConcurrencyException :
    SagaException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public ConcurrencyException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="sagaType">The saga type value.</param>
    /// <param name="correlationId">The correlation id value.</param>
    public ConcurrencyException(string message, Type sagaType, Guid correlationId)
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
    public ConcurrencyException(string message, Type sagaType, Guid correlationId, Exception innerException)
        : base(message, sagaType, correlationId, innerException)
    {
    }
}
