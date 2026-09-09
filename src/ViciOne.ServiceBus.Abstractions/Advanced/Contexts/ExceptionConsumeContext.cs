using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides a consume context together with the exception that interrupted its pipeline.</summary>
public interface ExceptionConsumeContext :
    ConsumeContext
{
    /// <summary>Gets the exception that interrupted consumption.</summary>
    Exception Exception { get; }

    /// <summary>Gets the serializable exception details included in fault messages.</summary>
    ExceptionInfo ExceptionInfo { get; }
}


/// <summary>Provides a typed consume context together with the exception that interrupted its pipeline.</summary>
/// <typeparam name="TMessage">The consumed message contract.</typeparam>
public interface ExceptionConsumeContext<out TMessage> :
    ExceptionConsumeContext,
    ConsumeContext<TMessage>
    where TMessage : class
{
}
