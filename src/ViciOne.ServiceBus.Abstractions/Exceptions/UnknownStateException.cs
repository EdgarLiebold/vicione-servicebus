using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to unknown state.
/// </summary>
public class UnknownStateException :
    SagaStateMachineException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public UnknownStateException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machineType">The machine type value.</param>
    /// <param name="stateName">The state name value.</param>
    public UnknownStateException(string machineType, string stateName)
        : base($"The {stateName} state is not defined for the {machineType} state machine")
    {
    }
}
