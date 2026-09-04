using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>
/// Defines the contract for exception consume context.
/// </summary>
public interface ExceptionConsumeContext :
    ConsumeContext
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


/// <summary>
/// Defines the contract for exception consume context.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public interface ExceptionConsumeContext<out T> :
    ExceptionConsumeContext,
    ConsumeContext<T>
    where T : class
{
}
