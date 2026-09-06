using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to unhandled event.</summary>
public class UnhandledEventException :
    SagaStateMachineException
{
    /// <summary>Initializes a new instance.</summary>
    public UnhandledEventException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machineType">The runtime machine type used by the operation.</param>
    /// <param name="eventName">The event name.</param>
    /// <param name="stateName">The state name.</param>
    public UnhandledEventException(string machineType, string eventName, string stateName)
        : base($"The {eventName} event is not handled during the {stateName} state for the {machineType} state machine")
    {
    }
}
