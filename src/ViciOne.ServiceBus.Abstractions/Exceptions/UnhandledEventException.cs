using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to unhandled event.
/// </summary>
[Serializable]
public class UnhandledEventException :
    SagaStateMachineException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public UnhandledEventException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machineType">The machine type value.</param>
    /// <param name="eventName">The event name value.</param>
    /// <param name="stateName">The state name value.</param>
    public UnhandledEventException(string machineType, string eventName, string stateName)
        : base($"The {eventName} event is not handled during the {stateName} state for the {machineType} state machine")
    {
    }
}
