using System;

#nullable enable
namespace ViciOne.ServiceBus.Testing;

/// <summary>
/// Specifies the available state machine event execution status values.
/// </summary>
public enum StateMachineEventExecutionStatus
{
    /// <summary>
    /// Indicates started.
    /// </summary>
    Started,
    /// <summary>
    /// Indicates completed.
    /// </summary>
    Completed,
    /// <summary>
    /// Indicates faulted.
    /// </summary>
    Faulted
}

/// <summary>
/// Represents a state machine event observation value.
/// </summary>
public sealed record StateMachineEventObservation(
    Guid SagaId,
    string EventName,
    Type? DataType,
    StateMachineEventExecutionStatus Status,
    Exception? Exception = null);

/// <summary>
/// Represents a state machine state change value.
/// </summary>
public sealed record StateMachineStateChange(
    Guid SagaId,
    string? PreviousState,
    string CurrentState);
