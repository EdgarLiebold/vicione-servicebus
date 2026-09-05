using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for exception saga consume context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ExceptionSagaConsumeContext<out T> :
    SagaConsumeContext<T>
    where T : class
{
    /// <summary>
    /// The exception that was thrown
    /// </summary>
    Exception Exception { get; }

    /// <summary>
    /// The exception info, suitable for inclusion in a fault message
    /// </summary>
    ExceptionInfo ExceptionInfo { get; }
}
