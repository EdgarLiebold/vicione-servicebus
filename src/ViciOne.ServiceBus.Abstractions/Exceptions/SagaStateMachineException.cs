using System;

namespace ViciOne.ServiceBus;

/// <summary>Provides the exception base for invalid saga state-machine definitions and behavior execution.</summary>
public class SagaStateMachineException :
    ViciOneServiceBusException
{
    /// <summary>Creates a state-machine exception without a custom message.</summary>
    public SagaStateMachineException()
    {
    }

    /// <summary>Creates a state-machine exception with the specified failure message.</summary>
    /// <param name="message">The description of the state-machine failure.</param>
    public SagaStateMachineException(string message)
        : base(message)
    {
    }

    /// <summary>Creates an exception for the specified state-machine type.</summary>
    /// <param name="machineType">The runtime machine type used by the operation.</param>
    /// <param name="message">The description of the state-machine failure.</param>
    public SagaStateMachineException(Type machineType, string message)
        : base(FormatMessage(machineType, message))
    {
        MachineType = machineType;
    }

    /// <summary>Creates a state-machine exception with an underlying failure.</summary>
    /// <param name="message">The description of the state-machine failure.</param>
    /// <param name="innerException">The exception raised by the state machine.</param>
    public SagaStateMachineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates an exception for the specified state-machine type and underlying failure.</summary>
    /// <param name="machineType">The runtime machine type used by the operation.</param>
    /// <param name="message">The description of the state-machine failure.</param>
    /// <param name="innerException">The exception raised by the state machine.</param>
    public SagaStateMachineException(Type machineType, string message, Exception innerException)
        : base(FormatMessage(machineType, message), innerException)
    {
        MachineType = machineType;
    }

    /// <summary>Gets the state-machine type associated with the failure, when one was supplied.</summary>
    public Type? MachineType { get; }

    static string FormatMessage(Type machineType, string message)
    {
        ArgumentNullException.ThrowIfNull(machineType);

        return $"{machineType.Name}: {message}";
    }
}
