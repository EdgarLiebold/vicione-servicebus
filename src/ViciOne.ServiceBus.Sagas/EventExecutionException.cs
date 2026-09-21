using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a failure while a saga state machine executes an event behavior.</summary>
public sealed class EventExecutionException :
    SagaStateMachineException
{
    /// <summary>Creates an event-execution exception with the specified failure message.</summary>
    /// <param name="message">The description of the event execution failure.</param>
    public EventExecutionException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an event-execution exception with an underlying failure.</summary>
    /// <param name="message">The description of the event execution failure.</param>
    /// <param name="innerException">The exception raised while executing the event behavior.</param>
    public EventExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates an event-execution exception without a custom message.</summary>
    public EventExecutionException()
    {
    }
}
