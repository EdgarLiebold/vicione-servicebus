namespace ViciOne.ServiceBus.Testing;

/// <summary>Describes an observed state-machine state transition.</summary>
/// <param name="SagaId">The saga instance identifier.</param>
/// <param name="PreviousState">The state before the transition, if one existed.</param>
/// <param name="CurrentState">The state after the transition.</param>
public sealed record StateMachineStateChange(
    Guid SagaId,
    string? PreviousState,
    string CurrentState);
