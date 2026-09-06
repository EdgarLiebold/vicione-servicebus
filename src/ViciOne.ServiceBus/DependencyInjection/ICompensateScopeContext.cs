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

    /// <summary>Gets service.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The service.</returns>
    T GetService<T>()
        where T : class;
}
