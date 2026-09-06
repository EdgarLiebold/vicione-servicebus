using System;

namespace ViciOne.ServiceBus;

/// <summary>
/// Represents an error related to saga state machine.
/// </summary>
public class SagaStateMachineException :
    ViciOneServiceBusException
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public SagaStateMachineException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    public SagaStateMachineException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machineType">The machine type value.</param>
    /// <param name="message">The message value.</param>
    public SagaStateMachineException(Type machineType, string message)
        : base($"{machineType.Name}: {message}")
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public SagaStateMachineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="machineType">The machine type value.</param>
    /// <param name="message">The message value.</param>
    /// <param name="innerException">The inner exception value.</param>
    public SagaStateMachineException(Type machineType, string message, Exception innerException)
        : base($"{machineType.Name}: {message}", innerException)
    {
    }
}
