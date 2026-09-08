namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Identifies the state-machine element represented by a graph node.</summary>
public enum StateMachineGraphNodeKind
{
    /// <summary>The node represents a state.</summary>
    State,

    /// <summary>The node represents an event.</summary>
    Event,

    /// <summary>The node represents an exception handled by a catch branch.</summary>
    Exception,
}
