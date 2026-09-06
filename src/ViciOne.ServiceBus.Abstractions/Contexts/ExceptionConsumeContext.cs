using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Exposes state for exception consume operations.</summary>
public interface ExceptionConsumeContext :
    ConsumeContext
{
    /// <summary>The exception that was thrown.</summary>
    Exception Exception { get; }

    /// <summary>The exception info, suitable for inclusion in a fault message.</summary>
    ExceptionInfo ExceptionInfo { get; }
}


/// <summary>Exposes state for exception consume operations.</summary>
/// <typeparam name="T">The value type.</typeparam>
public interface ExceptionConsumeContext<out T> :
    ExceptionConsumeContext,
    ConsumeContext<T>
    where T : class
{
}
