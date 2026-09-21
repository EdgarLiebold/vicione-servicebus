using System;

namespace ViciOne.ServiceBus;

/// <summary>Reports an event name that is not defined by a saga state machine.</summary>
public sealed class UnknownEventException :
    SagaStateMachineException,
    IRetryFailureClassification
{
    /// <summary>Creates an unknown-event exception without state-machine context.</summary>
    public UnknownEventException()
    {
    }

    /// <summary>Creates an exception for an event not defined by the specified state machine.</summary>
    /// <param name="machineName">The logical name of the state machine.</param>
    /// <param name="eventName">The undefined event name.</param>
    public UnknownEventException(string machineName, string eventName)
        : base(FormatMessage(machineName, eventName))
    {
        MachineName = machineName;
        EventName = eventName;
    }

    /// <summary>Gets the logical state-machine name, when one was supplied.</summary>
    public string? MachineName { get; }

    /// <summary>Gets the undefined event name, when one was supplied.</summary>
    public string? EventName { get; }

    RetryFailureKind IRetryFailureClassification.RetryFailureKind => RetryFailureKind.NonRetryable;

    static string FormatMessage(string machineName, string eventName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(machineName);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        return $"The {eventName} event is not defined for the {machineName} state machine";
    }
}
