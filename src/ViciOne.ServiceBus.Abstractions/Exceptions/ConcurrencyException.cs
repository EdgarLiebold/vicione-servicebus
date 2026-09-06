using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to concurrency.</summary>
public class ConcurrencyException :
    SagaException
{
    /// <summary>Initializes a new instance.</summary>
    public ConcurrencyException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    public ConcurrencyException(string message, Type sagaType, Guid correlationId)
        : base(message, sagaType, correlationId)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="correlationId">The correlation id.</param>
    /// <param name="innerException">The inner exception.</param>
    public ConcurrencyException(string message, Type sagaType, Guid correlationId, Exception innerException)
        : base(message, sagaType, correlationId, innerException)
    {
    }
}
