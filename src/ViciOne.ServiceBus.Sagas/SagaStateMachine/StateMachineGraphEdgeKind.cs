namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Identifies the relationship represented by a state-machine graph edge.</summary>
public enum StateMachineGraphEdgeKind
{
    /// <summary>A state directly handles or ignores an event.</summary>
    EventBinding,

    /// <summary>An event or exception branch transitions to a state.</summary>
    StateTransition,

    /// <summary>An event or exception branch enters an exception-handling branch.</summary>
    ExceptionHandler,

    /// <summary>An event or exception branch contributes to a composite event.</summary>
    CompositeContribution,

    /// <summary>A substate inherits the behavior configured by its superstate.</summary>
    StateInheritance,
}
