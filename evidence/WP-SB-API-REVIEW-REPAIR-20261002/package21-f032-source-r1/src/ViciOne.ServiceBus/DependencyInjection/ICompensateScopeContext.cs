using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Exposes state for compensate scope operations.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateScopeContext<out TLog> :
    IAsyncDisposable
    where TLog : class
{
    /// <summary>Gets the context.</summary>
    CompensateContext<TLog> Context { get; }

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
