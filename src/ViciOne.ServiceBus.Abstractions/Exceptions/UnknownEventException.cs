using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to unknown event.</summary>
public class UnknownEventException :
    SagaStateMachineException
{
    /// <summary>Initializes a new instance.</summary>
    public UnknownEventException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machineType">The runtime machine type used by the operation.</param>
    /// <param name="eventName">The event name.</param>
    public UnknownEventException(string machineType, string eventName)
        : base($"The {eventName} event is not defined for the {machineType} state machine")
    {
    }
}
