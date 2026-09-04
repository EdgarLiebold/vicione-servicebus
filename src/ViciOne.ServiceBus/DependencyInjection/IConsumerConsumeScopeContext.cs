using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for consumer consume scope context.
/// </summary>
/// <typeparam name="TConsumer">The t consumer type.</typeparam>
/// <typeparam name="T">The t type.</typeparam>
public interface IConsumerConsumeScopeContext<out TConsumer, out T> :
    IAsyncDisposable
    where T : class
    where TConsumer : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    ConsumerConsumeContext<TConsumer, T> Context { get; }
}
