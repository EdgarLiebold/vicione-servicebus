using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports a state name that is not defined by a saga state machine.</summary>
public sealed class UnknownStateException :
    SagaStateMachineException
{
    /// <summary>Creates an unknown-state exception without state-machine context.</summary>
    public UnknownStateException()
    {
    }

    /// <summary>Creates an exception for a state not defined by the specified state machine.</summary>
    /// <param name="machineName">The logical name of the state machine.</param>
    /// <param name="stateName">The undefined state name.</param>
    public UnknownStateException(string machineName, string stateName)
        : base(FormatMessage(machineName, stateName))
    {
        MachineName = machineName;
        StateName = stateName;
    }

    /// <summary>Gets the logical state-machine name, when one was supplied.</summary>
    public string? MachineName { get; }

    /// <summary>Gets the undefined state name, when one was supplied.</summary>
    public string? StateName { get; }

    static string FormatMessage(string machineName, string stateName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);

        return $"The {stateName} state is not defined for the {machineName} state machine";
    }
}
