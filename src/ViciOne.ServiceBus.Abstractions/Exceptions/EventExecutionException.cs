using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to event execution.
/// </summary>
public class EventExecutionException :
    SagaStateMachineException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public EventExecutionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public EventExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public EventExecutionException()
    {
    }
}
