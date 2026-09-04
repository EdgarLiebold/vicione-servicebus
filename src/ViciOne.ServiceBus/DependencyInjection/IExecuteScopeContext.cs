using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Defines the contract for execute scope context.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public interface IExecuteScopeContext<out TArguments> :
    IAsyncDisposable
    where TArguments : class
{
    /// <summary>
    /// Gets the context value.
    /// </summary>
    ExecuteContext<TArguments> Context { get; }

    /// <summary>
    /// Gets service.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <returns>The result of the operation.</returns>
    T GetService<T>()
        where T : class;
}
