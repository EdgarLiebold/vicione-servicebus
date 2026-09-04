using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to unknown event.
/// </summary>
[Serializable]
public class UnknownEventException :
    SagaStateMachineException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public UnknownEventException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machineType">The machine type value.</param>
    /// <param name="eventName">The event name value.</param>
    public UnknownEventException(string machineType, string eventName)
        : base($"The {eventName} event is not defined for the {machineType} state machine")
    {
    }
}
