using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides a saga instance and the exception that interrupted its consume pipeline.</summary>
/// <typeparam name="TSaga">The saga state type.</typeparam>
public interface ExceptionSagaConsumeContext<out TSaga> :
    SagaConsumeContext<TSaga>
    where TSaga : class
{
    /// <summary>Gets the exception that interrupted saga consumption.</summary>
    Exception Exception { get; }

    /// <summary>Gets the serializable exception details included in fault messages.</summary>
    ExceptionInfo ExceptionInfo { get; }
}
