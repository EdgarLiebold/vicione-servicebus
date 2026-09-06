using System;

namespace ViciOne.ServiceBus;

/// <summary>Represents an error related to saga state machine.</summary>
public class SagaStateMachineException :
    ViciOneServiceBusException
{
    /// <summary>Initializes a new instance.</summary>
    public SagaStateMachineException()
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    public SagaStateMachineException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machineType">The runtime machine type used by the operation.</param>
    /// <param name="message">The message to process.</param>
    public SagaStateMachineException(Type machineType, string message)
        : base($"{machineType.Name}: {message}")
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public SagaStateMachineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="machineType">The runtime machine type used by the operation.</param>
    /// <param name="message">The message to process.</param>
    /// <param name="innerException">The inner exception.</param>
    public SagaStateMachineException(Type machineType, string message, Exception innerException)
        : base($"{machineType.Name}: {message}", innerException)
    {
    }
}
