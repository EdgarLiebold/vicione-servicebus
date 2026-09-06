using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to event execution.</summary>
public class EventExecutionException :
    SagaStateMachineException
{
    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public EventExecutionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public EventExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    public EventExecutionException()
    {
    }
}
