using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for execute scope operations.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteScopeContext<out TArguments> :
    IAsyncDisposable
    where TArguments : class
{
    /// <summary>Gets the context.</summary>
    ExecuteContext<TArguments> Context { get; }

    /// <summary>Resolves a service or creates a fallback instance.</summary>
    /// <remarks>
    /// The default provider borrows registered services and owns each fallback created by this method.
    /// It releases fallbacks asynchronously when supported, before restoring the context and releasing an owned DI scope.
    /// Closing rejects new resolutions and waits for admitted resolutions and cleanup; repeated disposal awaits the same outcome.
    /// </remarks>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The service.</returns>
    T GetService<T>()
        where T : class;
}
