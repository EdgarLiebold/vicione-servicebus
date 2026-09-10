namespace ViciOne.ServiceBus.Testing;

/// <summary>Describes an observed state-machine event execution.</summary>
/// <param name="SagaId">The saga correlation identifier.</param>
/// <param name="EventName">The state-machine event name.</param>
/// <param name="DataType">The event data type, or <see langword="null"/> for an event without data.</param>
/// <param name="Status">The observed execution status.</param>
/// <param name="Exception">The execution exception for a faulted event, or <see langword="null"/>.</param>
public sealed record StateMachineEventObservation(
    Guid SagaId,
    string EventName,
    Type? DataType,
    StateMachineEventExecutionStatus Status,
    Exception? Exception = null);
