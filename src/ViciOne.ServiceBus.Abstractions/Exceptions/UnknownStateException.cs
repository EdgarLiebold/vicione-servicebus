using System;

namespace ViciOne.ServiceBus;

[Serializable]
public class UnknownStateException :
    SagaStateMachineException
{
    public UnknownStateException()
    {
    }

    public UnknownStateException(string machineType, string stateName)
        : base($"The {stateName} state is not defined for the {machineType} state machine")
    {
    }
}
