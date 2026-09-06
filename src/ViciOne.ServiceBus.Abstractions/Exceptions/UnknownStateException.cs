using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to unknown state.</summary>
public class UnknownStateException :
    SagaStateMachineException
{
    /// <summary>Initializes a new instance.</summary>
    public UnknownStateException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machineType">The runtime machine type used by the operation.</param>
    /// <param name="stateName">The state name.</param>
    public UnknownStateException(string machineType, string stateName)
        : base($"The {stateName} state is not defined for the {machineType} state machine")
    {
    }
}
