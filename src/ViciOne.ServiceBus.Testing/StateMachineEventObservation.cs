namespace ViciOne.ServiceBus.Testing;

/// <summary>Describes an observed state-machine event execution.</summary>
/// <param name="SagaId">The saga id.</param>
/// <param name="EventName">The event name.</param>
/// <param name="DataType">The runtime data type used by the operation.</param>
/// <param name="Status">The status.</param>
/// <param name="Exception">The exception.</param>
public sealed record StateMachineEventObservation(
    Guid SagaId,
    string EventName,
    Type? DataType,
    StateMachineEventExecutionStatus Status,
    Exception? Exception = null);
