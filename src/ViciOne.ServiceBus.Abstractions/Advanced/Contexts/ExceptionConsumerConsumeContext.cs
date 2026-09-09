using System;

namespace ViciOne.ServiceBus.Advanced;

/// <summary>Provides a consumer instance and the exception that interrupted its consume pipeline.</summary>
/// <typeparam name="TConsumer">The consumer implementation.</typeparam>
public interface ExceptionConsumerConsumeContext<out TConsumer> :
    ConsumerConsumeContext<TConsumer>
    where TConsumer : class
{
    /// <summary>Gets the exception that interrupted consumption.</summary>
    Exception Exception { get; }

    /// <summary>Gets the serializable exception details included in fault messages.</summary>
    ExceptionInfo ExceptionInfo { get; }
}
