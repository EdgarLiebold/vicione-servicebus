using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports an event for which the current saga state has no configured behavior.</summary>
public sealed class UnhandledEventException :
    SagaStateMachineException,
    IRetryFailureClassification
{
    /// <summary>Creates an unhandled-event exception without state-machine context.</summary>
    public UnhandledEventException()
    {
    }

    /// <summary>Creates an exception for an event not handled in the specified state.</summary>
    /// <param name="machineName">The logical name of the state machine.</param>
    /// <param name="eventName">The unhandled event name.</param>
    /// <param name="stateName">The state in which the event was received.</param>
    public UnhandledEventException(string machineName, string eventName, string stateName)
        : base(FormatMessage(machineName, eventName, stateName))
    {
        MachineName = machineName;
        EventName = eventName;
        StateName = stateName;
    }

    /// <summary>Gets the logical state-machine name, when one was supplied.</summary>
    public string? MachineName { get; }

    /// <summary>Gets the unhandled event name, when one was supplied.</summary>
    public string? EventName { get; }

    /// <summary>Gets the state in which the event was received, when one was supplied.</summary>
    public string? StateName { get; }

    RetryFailureKind IRetryFailureClassification.RetryFailureKind => RetryFailureKind.NonRetryable;

    static string FormatMessage(string machineName, string eventName, string stateName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentException.ThrowIfNullOrWhiteSpace(stateName);

        return $"The {eventName} event is not handled during the {stateName} state for the {machineName} state machine";
    }
}
