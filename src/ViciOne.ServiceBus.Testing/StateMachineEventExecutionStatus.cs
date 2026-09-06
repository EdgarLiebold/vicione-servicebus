namespace ViciOne.ServiceBus.Testing;

/// <summary>Specifies the lifecycle state of an observed state-machine event.</summary>
public enum StateMachineEventExecutionStatus
{
    /// <summary>Indicates started.</summary>
    Started,

    /// <summary>Indicates completed.</summary>
    Completed,

    /// <summary>Indicates faulted.</summary>
    Faulted,
}
