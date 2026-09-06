namespace ViciOne.ServiceBus.Testing;

/// <summary>Describes an observed state-machine event execution.</summary>
/// <param name="SagaId">The saga instance identifier.</param>
/// <param name="EventName">The state-machine event name.</param>
/// <param name="DataType">The optional event data type.</param>
/// <param name="Status">The observed execution status.</param>
/// <param name="Exception">The exception when execution faulted.</param>
public sealed record StateMachineEventObservation(
    Guid SagaId,
    string EventName,
    Type? DataType,
    StateMachineEventExecutionStatus Status,
    Exception? Exception = null);
