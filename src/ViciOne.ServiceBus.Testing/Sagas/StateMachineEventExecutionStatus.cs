namespace ViciOne.ServiceBus.Testing;

/// <summary>Specifies the lifecycle state of an observed state-machine event.</summary>
public enum StateMachineEventExecutionStatus
{
    /// <summary>The event behavior has started.</summary>
    Started,

    /// <summary>The event behavior completed successfully.</summary>
    Completed,

    /// <summary>The event behavior faulted.</summary>
    Faulted,
}
