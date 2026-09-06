using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for consumer consume scope operations.</summary>
/// <typeparam name="TConsumer">The consumer implementation used by the member.</typeparam>
/// <typeparam name="T">The value type.</typeparam>
public interface IConsumerConsumeScopeContext<out TConsumer, out T> :
    IAsyncDisposable
    where T : class
    where TConsumer : class
{
    /// <summary>Gets the context.</summary>
    ConsumerConsumeContext<TConsumer, T> Context { get; }
}
