#nullable enable
namespace ViciOne.ServiceBus.Testing;

using System;

public enum StateMachineEventExecutionStatus
{
    Started,
    Completed,
    Faulted
}

public sealed record StateMachineEventObservation(
    Guid SagaId,
    string EventName,
    Type? DataType,
    StateMachineEventExecutionStatus Status,
    Exception? Exception = null);

public sealed record StateMachineStateChange(
    Guid SagaId,
    string? PreviousState,
    string CurrentState);
