namespace ViciOne.ServiceBus.Testing;

/// <summary>Specifies the lifecycle state of an observed state-machine event.</summary>
public enum StateMachineEventExecutionStatus
{
    /// <summary>The event execution has started.</summary>
    Started,

    /// <summary>The event execution completed successfully.</summary>
    Completed,

    /// <summary>The event execution faulted.</summary>
    Faulted,
}
