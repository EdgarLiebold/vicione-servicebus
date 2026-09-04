using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for compensate scope context.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface ICompensateScopeContext<out TLog> :
    IAsyncDisposable
    where TLog : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    CompensateContext<TLog> Context { get; }

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    T GetService<T>()
        where T : class;
}
