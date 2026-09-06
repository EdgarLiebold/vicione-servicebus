using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for exception saga consume operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ExceptionSagaConsumeContext<out T> :
    SagaConsumeContext<T>
    where T : class
{
    /// <summary>The exception that was thrown.</summary>
    Exception Exception { get; }

    /// <summary>The exception info, suitable for inclusion in a fault message.</summary>
    ExceptionInfo ExceptionInfo { get; }
}
