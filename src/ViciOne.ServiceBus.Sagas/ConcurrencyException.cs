using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a transient optimistic-concurrency conflict while persisting a saga instance.</summary>
public class ConcurrencyException :
    SagaException,
    IRetryFailureClassification
{
    /// <summary>Creates a concurrency exception without saga context.</summary>
    public ConcurrencyException()
    {
    }

    /// <summary>Creates a concurrency exception for the specified saga instance.</summary>
    /// <param name="message">The description of the concurrency conflict.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="correlationId">The identifier of the conflicting saga instance.</param>
    public ConcurrencyException(string message, Type sagaType, Guid correlationId)
        : base(message, sagaType, correlationId)
    {
    }

    /// <summary>Creates a concurrency exception for the specified saga instance and persistence failure.</summary>
    /// <param name="message">The description of the concurrency conflict.</param>
    /// <param name="sagaType">The runtime saga type used by the operation.</param>
    /// <param name="correlationId">The identifier of the conflicting saga instance.</param>
    /// <param name="innerException">The exception raised by the saga repository.</param>
    public ConcurrencyException(string message, Type sagaType, Guid correlationId, Exception innerException)
        : base(message, sagaType, correlationId, innerException)
    {
    }

    RetryFailureKind IRetryFailureClassification.RetryFailureKind => RetryFailureKind.Transient;
}
